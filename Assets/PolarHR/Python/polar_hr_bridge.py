"""
Polar HR Bridge — scans for a Polar BLE device, streams heart rate over UDP.
Requires: pip install bleak
"""

import asyncio
import socket
import struct
from datetime import datetime

from bleak import BleakScanner, BleakClient

HR_CHAR_UUID = "00002a37-0000-1000-8000-00805f9b34fb"
UDP_IP = "127.0.0.1"
UDP_PORT = 12345

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)


def hr_notification_handler(sender, data: bytearray):
    flags = data[0]
    if flags & 0x01:
        bpm = struct.unpack_from("<H", data, 1)[0]
    else:
        bpm = data[1]

    ts = datetime.now().strftime("%H:%M:%S.%f")[:-3]
    print(f"[HR] {ts} — {bpm} bpm")
    sock.sendto(str(bpm).encode(), (UDP_IP, UDP_PORT))


async def main():
    print("[HR Bridge] Scanning for Polar device...")
    device = None

    devices = await BleakScanner.discover(timeout=10.0)
    for d in devices:
        if d.name and "Polar" in d.name:
            device = d
            print(f"[HR Bridge] Found: {d.name} ({d.address})")
            break

    if device is None:
        print("[HR Bridge] No Polar device found. Wear the sensor and try again.")
        return

    async with BleakClient(device.address) as client:
        print(f"[HR Bridge] Connected to {device.name}. Streaming HR...")
        await client.start_notify(HR_CHAR_UUID, hr_notification_handler)
        await asyncio.sleep(3600)  # run for up to 1 hour; Unity kills the process on quit
        await client.stop_notify(HR_CHAR_UUID)


if __name__ == "__main__":
    asyncio.run(main())
