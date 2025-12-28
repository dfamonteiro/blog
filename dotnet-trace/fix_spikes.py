import json
import argparse
from pathlib import Path
from typing import List, Dict, Any

def find_discontinuities(trace_events : List[Dict[str, Any]]) -> List[int]:
    """Finds all sequences of 100 "Begin" trace events that share the same timestamp

    Returns:
        List[int]: List of indices that point to the start of discontinuities or "spikes" in the list of trace events
    """
    res = []

    candidate = None
    for index, event in enumerate(trace_events):
        if (event["ph"]) == "E":
            candidate = None

        elif candidate == None:
            candidate = (index, event["ts"])

        elif index - candidate[0] == 100:
            # We found a spike!
            res.append(candidate[0])
            candidate = None
        
        elif candidate[1] != event["ts"]:
            # If the timestamp doesn't match, reset the candidate
            candidate = (index, event["ts"])

    return res

def get_missing_stack_frames(index : int, trace_events : List[Dict[str, Any]]) -> List[str]:
    """Returns the missing stack frames for a given spike

    Returns:
        List[str]: A list of the missing stack frame names
    """
    spike_base_name = trace_events[index]["name"]
    res = []

    while True:
        index -= 1
        current_name = trace_events[index]["name"]

        if current_name == spike_base_name:
            return res
        else:
            res.append(current_name)

def find_matching_trace_event(index : int, trace_events : List[Dict[str, Any]]) -> int:
    """Finds the matching trace event.

    Returns:
        int: A pointer to the matching trace event
    """
    depth = 0

    while True:
        index += 1

        if trace_events[index]["ph"] == "B":
            depth += 1
        else:
            depth -= 1
        
        if depth == 0:
            return index + 1

def add_missing_stack_frames_to_spike(spike_pointer : int, trace_events : List[Dict[str, Any]]) -> int:
    """Adds the missing stack frames to the spike pointed by spike_pointer.

    Returns:
        int: The number of added trace events
    """
    end_index = find_matching_trace_event(spike_pointer, trace_events)

    begin_ts = trace_events[spike_pointer]["ts"]
    end_ts   = trace_events[end_index]["ts"]

    pid = trace_events[spike_pointer]["pid"]
    tid = trace_events[spike_pointer]["tid"]

    stack_frames = get_missing_stack_frames(spike_pointer, trace_events)

    # Add the Begin trace events
    for name in reversed(stack_frames):
        trace_events.insert(spike_pointer, {
            "name" : name,
            "ph" : "B",
            "ts" : begin_ts,
            "pid" : pid,
            "tid" : tid,
        })
    end_index += len(stack_frames) # Update the end index to reflect the newly added events

    # Add the End trace events
    for name in reversed(stack_frames):
        trace_events.insert(end_index + 1, {
            "name" : name,
            "ph" : "E",
            "ts" : end_ts,
            "pid" : pid,
            "tid" : tid,
        })
        end_index += 1 # Update the end index
    
    return len(stack_frames) * 2

def merge_spans(spike_pointers : List[int], trace_events : List[Dict[str, Any]]):
    """Merge contiguous spans that share the same name, tid, pid, and timestamp.
    It takes into consideration the location of trace file's known spikes to
    avoid accidentally merging spans that were not meant to be merged.
    """
    
    # Collection of timestamps around which you should be safe merging spans
    # The timestamps are organized per thread ID
    spike_timestamps : Dict[int, List[float]] = {}

    # Populate spike_timestamps
    for pointer in spike_pointers:
        tid = trace_events[pointer]["tid"]
        matching_event = find_matching_trace_event(pointer, trace_events)

        if tid not in spike_timestamps:
            spike_timestamps[tid] = []
        
        spike_timestamps[tid].append(trace_events[pointer]["ts"])
        spike_timestamps[tid].append(trace_events[matching_event]["ts"])
    
    index = 1
    while index < len(trace_events):
        current  = trace_events[index]
        previous = trace_events[index - 1]

        event_types_are_correct = (previous["ph"], current["ph"]) == ("E", "B")
        same_name = current["name"] == previous["name"]
        same_tid  = current["tid"]  == previous["tid"]
        same_pid  = current["pid"]  == previous["pid"]
        timestamps_match = current["ts"] - previous["ts"] < 50 # Within 50 us

        # Final validation: check that the event is within 0.5ms of a spike
        event_is_close_to_a_spike = False
        for ts in spike_timestamps.get(current["tid"], []):
            if abs(ts - current["ts"]) < 500:
                event_is_close_to_a_spike = True
                break
        
        all_conditions_are_met = all((
            event_types_are_correct, 
            same_name, 
            same_tid, 
            same_pid, 
            timestamps_match, 
            event_is_close_to_a_spike,
        ))

        if all_conditions_are_met:
            trace_events.pop(index - 1)
            trace_events.pop(index - 1)
            index -= 1
        else:
            index += 1

def fix_spikes(trace_file: Dict[str, Any]):
    "Fix the spikes/discontinuities in the trace file"

    # These pointers are guaranteed to be ordered from lowest to highest
    spike_pointers = find_discontinuities(trace_file["traceEvents"])

    for i in range(len(spike_pointers)):
        offset = add_missing_stack_frames_to_spike(spike_pointers[i], trace_file["traceEvents"])

        # Update the necessary spike pointers
        for j in range(i + 1, len(spike_pointers)):
            spike_pointers[j] += offset
    
    merge_spans(spike_pointers, trace_file["traceEvents"])

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        prog='fix_spikes',
        description='Fixes spikes in dotnet-trace trace files that occur when the number of stack frames goes over 100'
    )
    parser.add_argument('filename')
    filename = Path(parser.parse_args().filename).absolute()

    with open(filename) as f: 
        trace_file : dict = json.load(f)

    fix_spikes(trace_file)
    
    with open(filename.parent / f"fixed_{filename.name}", "w") as f: 
        json.dump(trace_file, f)