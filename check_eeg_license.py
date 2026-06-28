"""
check_eeg_license.py
Connects to Emotiv Cortex and prints whether the current account/credentials
support raw EEG recording.

Run with:
    "D:\Python 3.10\python.exe" "D:\Final_Objective1_Validation\check_eeg_license.py"

Make sure Emotiv Cortex (EMOTIV Launcher) is running before you run this.
"""

import asyncio
import json
import ssl
import websockets

# ── Fill these in (same values as your Unity Inspector) ──────────────────────
CLIENT_ID     = ""   # paste your client ID here
CLIENT_SECRET = ""   # paste your client secret here
# ─────────────────────────────────────────────────────────────────────────────

CORTEX_URL = "wss://localhost:6868"
_id = 0

def next_id():
    global _id
    _id += 1
    return _id

async def send(ws, method, params):
    msg = {"id": next_id(), "jsonrpc": "2.0", "method": method, "params": params}
    await ws.send(json.dumps(msg))
    resp = json.loads(await ws.recv())
    return resp

async def main():
    ssl_ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    ssl_ctx.check_hostname = False
    ssl_ctx.verify_mode    = ssl.CERT_NONE

    print(f"Connecting to {CORTEX_URL} ...")
    async with websockets.connect(CORTEX_URL, ssl=ssl_ctx) as ws:

        # 1. Authorize — debit 1 session so we can check activation scope
        print("Authorizing ...")
        resp = await send(ws, "authorize", {
            "clientId":     CLIENT_ID,
            "clientSecret": CLIENT_SECRET,
            "debit":        1
        })

        if "error" in resp:
            print(f"\n[FAIL] authorize error: {resp['error']}")
            return

        token = resp["result"]["cortexToken"]
        warning = resp["result"].get("warning", {})
        if warning:
            print(f"[WARN] {warning.get('message', warning)}")
        print(f"[OK] Authorized. Token: {token[:20]}...")

        # 2. Get license info
        print("\nFetching license info ...")
        resp = await send(ws, "getLicenseInfo", {"cortexToken": token})

        if "error" in resp:
            print(f"[FAIL] getLicenseInfo error: {resp['error']}")
        else:
            info = resp.get("result", {})
            quota     = info.get("quota",    {})
            hard_lim  = info.get("hardLimitObj", {})
            scopes    = info.get("scopes",   [])
            tier      = info.get("tier",     "unknown")

            print(f"\n{'='*50}")
            print(f"  Licence tier : {tier}")
            print(f"  Scopes       : {scopes if scopes else '(none returned)'}")
            print(f"  Sessions used: {quota.get('usedSlots', '?')} / {quota.get('totalSlots', '?')}")
            print(f"{'='*50}")

            if "eeg" in scopes:
                print("\n  ✓ RAW EEG  — SUPPORTED  (scope \"eeg\" present)")
            else:
                print("\n  ✗ RAW EEG  — NOT available on this licence")

            if "pm" in scopes:
                print("  ✓ PM 2 Hz  — SUPPORTED  (scope \"pm\" present)")
            else:
                print("  ✗ PM       — low-res only (0.1 Hz), scope \"pm\" missing")

        print("\nDone.")

asyncio.run(main())
