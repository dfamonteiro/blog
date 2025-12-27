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

if __name__ == "__main__":
    with open(Path(__file__).parent / "a.chromium.json") as f: 
        trace_file : dict = json.load(f)
    
    # for i in find_discontinuities(trace_file["traceEvents"]):
    #     event = trace_file["traceEvents"][i]
    #     print(f"Index: {i}, Thread: {event["tid"]}, Timestamp: {round(event["ts"] / 1_000_000, 2)}s, Name: {event["name"]}")
    
    for i in find_discontinuities(trace_file["traceEvents"]):
        print(get_missing_stack_frames(i, trace_file["traceEvents"]))