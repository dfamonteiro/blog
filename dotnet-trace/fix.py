import json
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

if __name__ == "__main__":
    with open(Path(__file__).parent / "a.chromium.json") as f: 
        trace_file : dict = json.load(f)
    
    # for i in find_discontinuities(trace_file["traceEvents"]):
    #     event = trace_file["traceEvents"][i]
    #     print(f"Index: {i}, Thread: {event["tid"]}, Timestamp: {round(event["ts"] / 1_000_000, 2)}s, Name: {event["name"]}")
    
    # for i in find_discontinuities(trace_file["traceEvents"]):
    #     print(get_missing_stack_frames(i, trace_file["traceEvents"]))

    add_missing_stack_frames_to_spike(10456, trace_file["traceEvents"])

    
    with open(Path(__file__).parent / "fixed.chromium.json", "w") as f: 
        json.dump(trace_file, f)