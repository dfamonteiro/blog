import json
import hashlib

def anonymize_function(signature: str) -> str:
    # Encodes the string to bytes and creates a hex digest
    # Using blake2b for speed, but md5 or sha256 also work
    return hashlib.blake2b(signature.encode(), digest_size=8).hexdigest()

with open("a.chromium.json") as f: 
    trace_file : dict = json.load(f)

trace_file.pop("stackFrames")

trace_file["traceEvents"] = list(filter(lambda t: t["tid"] == 409, trace_file["traceEvents"]))

for trace_event in trace_file["traceEvents"]:
    name : str = trace_event["name"]
    if name.startswith("Cmf") and ".Services." in name and "Controller." in name:
        trace_event["name"] = "Service"
    else:
        trace_event["name"] = anonymize_function(name)

with open("scrambled.chromium.json", "w") as f: 
    json.dump(trace_file, f, indent="    ")