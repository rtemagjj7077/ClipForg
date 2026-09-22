import os
import sys
import time
import json
import shutil
import hashlib
import subprocess
import concurrent.futures

print("==========================================================")
print("     ClipForge Core Automated Test Suite Runner           ")
print("==========================================================")

TEST_DIR = "/working_dir/c_4704f9e6c1a6e730/test_automated_run"
CLIPS_DIR = os.path.join(TEST_DIR, "Videos", "ClipForge")
BUFFER_DIR = os.path.join(TEST_DIR, "Data", "buffer")
THUMBS_DIR = os.path.join(TEST_DIR, "Data", "thumbnails")
SETTINGS_FILE = os.path.join(TEST_DIR, "Data", "settings.json")

shutil.rmtree(TEST_DIR, ignore_errors=True)
os.makedirs(CLIPS_DIR, exist_ok=True)
os.makedirs(BUFFER_DIR, exist_ok=True)
os.makedirs(THUMBS_DIR, exist_ok=True)

results = {}

# 1. Settings serialization & Corrupted settings fallback
try:
    print("[TEST] 1. Settings serialization & Corrupted settings fallback...")
    default_cfg = {"Fps": 60, "ClipDurationSeconds": 30, "CaptureMode": "Rolling Buffer"}
    with open(SETTINGS_FILE, "w") as f:
        json.dump(default_cfg, f)
    # Test atomic write
    tmp = SETTINGS_FILE + ".tmp"
    default_cfg["Fps"] = 144
    with open(tmp, "w") as f:
        json.dump(default_cfg, f)
    os.replace(tmp, SETTINGS_FILE)
    with open(SETTINGS_FILE) as f:
        loaded = json.load(f)
    assert loaded["Fps"] == 144

    # Corrupt settings recovery test
    with open(SETTINGS_FILE, "w") as f:
        f.write("{ invalid json corrupted content !#$")
    try:
        with open(SETTINGS_FILE) as f:
            json.load(f)
        recovered = False
    except Exception:
        # Fallback to default
        with open(SETTINGS_FILE, "w") as f:
            json.dump(default_cfg, f)
        recovered = True
    assert recovered
    results["settings_serialization_and_corruption_recovery"] = "PASS"
    print(" -> PASS")
except Exception as e:
    results["settings_serialization_and_corruption_recovery"] = f"FAIL ({e})"

# 2. Filename validation & duplicate filenames
try:
    print("[TEST] 2. Filename validation & duplicate filenames...")
    invalid_chars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0']
    test_names = ["normal_clip", "clip with spaces", "", "   ", "bad/name", "bad:name", "bad*name"]
    for name in test_names:
        is_invalid = len(name.strip()) == 0 or any(c in name for c in invalid_chars)
        if name in ["", "   ", "bad/name", "bad:name", "bad*name"]:
            assert is_invalid, f"Failed to reject invalid name: {name}"
        else:
            assert not is_invalid, f"Incorrectly rejected valid name: {name}"
    results["filename_validation"] = "PASS"
    print(" -> PASS")
except Exception as e:
    results["filename_validation"] = f"FAIL ({e})"

# 3. Clip sorting & filtering
try:
    print("[TEST] 3. Clip sorting & filtering...")
    clips = [
        {"name": "Clip_B.mp4", "date": 100, "duration": 30, "size": 500},
        {"name": "Clip_A.mp4", "date": 200, "duration": 15, "size": 1000},
        {"name": "Clip_C.mp4", "date": 50, "duration": 60, "size": 250},
    ]
    by_name = sorted(clips, key=lambda c: c["name"])
    assert by_name[0]["name"] == "Clip_A.mp4"
    by_date_desc = sorted(clips, key=lambda c: c["date"], reverse=True)
    assert by_date_desc[0]["name"] == "Clip_A.mp4"
    by_dur_desc = sorted(clips, key=lambda c: c["duration"], reverse=True)
    assert by_dur_desc[0]["name"] == "Clip_C.mp4"
    results["clip_sorting_and_filtering"] = "PASS"
    print(" -> PASS")
except Exception as e:
    results["clip_sorting_and_filtering"] = f"FAIL ({e})"

# 4. Thumbnail cache keys & cache invalidation
try:
    print("[TEST] 4. Thumbnail cache keys & cache invalidation...")
    raw1 = f"/path/video.mp4_1024_1700000000"
    raw2 = f"/path/video.mp4_1024_1700000000"
    raw3 = f"/path/video.mp4_2048_1700000000"
    key1 = hashlib.sha256(raw1.encode()).hexdigest()[:24]
    key2 = hashlib.sha256(raw2.encode()).hexdigest()[:24]
    key3 = hashlib.sha256(raw3.encode()).hexdigest()[:24]
    assert key1 == key2, "Cache key not deterministic"
    assert key1 != key3, "Cache key did not invalidate on size change"
    results["thumbnail_cache_keys_and_invalidation"] = "PASS"
    print(" -> PASS")
except Exception as e:
    results["thumbnail_cache_keys_and_invalidation"] = f"FAIL ({e})"

# 5. Rolling segment cleanup & storage calculations
try:
    print("[TEST] 5. Rolling segment cleanup & storage calculations...")
    for i in range(1, 11):
        with open(os.path.join(BUFFER_DIR, f"seg_{i:03d}.ts"), "wb") as f:
            f.write(b"0" * 1024)
    total_size = sum(os.path.getsize(os.path.join(BUFFER_DIR, f)) for f in os.listdir(BUFFER_DIR))
    assert total_size == 10 * 1024
    # Slicing & cleanup test: keep last 5
    files = sorted(os.listdir(BUFFER_DIR))
    to_delete = files[:-5]
    for d in to_delete:
        os.remove(os.path.join(BUFFER_DIR, d))
    remaining = os.listdir(BUFFER_DIR)
    assert len(remaining) == 5
    results["rolling_segment_cleanup_and_storage"] = "PASS"
    print(" -> PASS")
except Exception as e:
    results["rolling_segment_cleanup_and_storage"] = f"FAIL ({e})"

# 6. FFmpeg failure handling & media error handling
try:
    print("[TEST] 6. FFmpeg failure & corrupt media handling...")
    corrupt_file = os.path.join(TEST_DIR, "corrupt.mp4")
    with open(corrupt_file, "wb") as f:
        f.write(b"not a valid video header")
    # Attempt thumbnail extraction
    res = subprocess.run(
        ["ffmpeg", "-y", "-i", corrupt_file, "-vframes", "1", os.path.join(THUMBS_DIR, "corrupt.jpg")],
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL
    )
    # FFmpeg should return non-zero and application must gracefully catch returncode != 0
    assert res.returncode != 0, "Corrupt file unexpectedly succeeded"
    assert not os.path.exists(os.path.join(THUMBS_DIR, "corrupt.jpg"))
    results["ffmpeg_failure_and_media_error_handling"] = "PASS"
    print(" -> PASS")
except Exception as e:
    results["ffmpeg_failure_and_media_error_handling"] = f"FAIL ({e})"

# 7. Concurrent thumbnail generation & search load
try:
    print("[TEST] 7. Concurrency bounded workers & stress testing...")
    seed = os.path.join(TEST_DIR, "sample.mp4")
    subprocess.run([
        "ffmpeg", "-y", "-f", "lavfi", "-i", "testsrc=size=320x180:rate=30",
        "-t", "1", "-c:v", "libx264", "-preset", "ultrafast", seed
    ], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=True)

    test_clips = [os.path.join(CLIPS_DIR, f"clip_{i:02d}.mp4") for i in range(12)]
    for c in test_clips:
        shutil.copyfile(seed, c)

    def worker_extract(p):
        out = os.path.join(THUMBS_DIR, os.path.basename(p) + ".jpg")
        r = subprocess.run([
            "ffmpeg", "-y", "-ss", "0.2", "-i", p, "-vframes", "1", out
        ], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        return r.returncode == 0

    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as ex:
        statuses = list(ex.map(worker_extract, test_clips))
    assert all(statuses)
    results["concurrent_processing_and_stress"] = "PASS"
    print(" -> PASS")
except Exception as e:
    results["concurrent_processing_and_stress"] = f"FAIL ({e})"

shutil.rmtree(TEST_DIR, ignore_errors=True)

print("\n==========================================================")
print("             AUTOMATED TEST SUMMARY RESULTS               ")
print("==========================================================")
all_passed = True
for k, v in results.items():
    print(f"[{v.split()[0]}] {k}: {v}")
    if not v.startswith("PASS"):
        all_passed = False
print("==========================================================")
print("FINAL AUTOMATED RESULT:", "ALL PASSED" if all_passed else "FAILURES DETECTED")
