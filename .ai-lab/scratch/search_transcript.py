import json, sys
sys.stdout.reconfigure(encoding='utf-8')

with open(r'C:\Users\HP\.gemini\antigravity\brain\2718d163-fe44-4901-b34e-d106d2d7ade9\.system_generated\logs\transcript.jsonl', 'r', encoding='utf-8') as f:
    for line_idx, line in enumerate(f):
        if line_idx in range(8241, 8246):
            obj = json.loads(line)
            content = str(obj.get('content', ''))[:400]
            print(f"[{line_idx}] {obj.get('type')}: {content}")
