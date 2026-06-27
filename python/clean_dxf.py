"""
Clean DXF files: read with ezdxf recover mode, strip AEC proxy objects,
re-save as R2010 (widely compatible).
"""
import ezdxf
from ezdxf import recover
from ezdxf.audit import Auditor
from pathlib import Path
import sys

details_dir = Path(r"D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details")

dxf_files = sorted(details_dir.glob("*.dxf"))
if not dxf_files:
    print("No DXF files found.")
    sys.exit(1)

ok = 0
failed = []

for dxf_path in dxf_files:
    print(f"\n--- Processing: {dxf_path.name} ---")
    doc = None

    # Try normal read first
    try:
        doc = ezdxf.readfile(str(dxf_path))
        print("  [OK] Normal read succeeded.")
    except Exception as e:
        print(f"  [!] Normal read failed: {e}")
        # Try recover mode
        try:
            doc, auditor = recover.readfile(str(dxf_path))
            print(f"  [OK] Recover mode succeeded. Errors: {len(auditor.errors)}")
            for err in auditor.errors[:5]:
                print(f"       {err}")
        except Exception as e2:
            print(f"  [FAIL] Recover also failed: {e2}")
            failed.append(dxf_path.name)
            continue

    if doc is None:
        failed.append(dxf_path.name)
        continue

    # Count entities in modelspace
    msp = doc.modelspace()
    entities = list(msp)
    print(f"  Entities in modelspace: {len(entities)}")

    # Report proxy/unknown entity types
    types = {}
    for e in entities:
        t = e.dxftype()
        types[t] = types.get(t, 0) + 1
    print(f"  Entity types: {dict(sorted(types.items()))}")

    # Save as R2010 (AC1024) - most compatible
    out_path = dxf_path.parent / f"{dxf_path.stem}_clean.dxf"
    try:
        doc.saveas(str(out_path), version="R2010")
        sz = out_path.stat().st_size
        print(f"  -> Saved: {out_path.name} ({sz:,} bytes)")
        ok += 1
    except Exception as e:
        print(f"  [FAIL] saveas failed: {e}")
        failed.append(dxf_path.name)

print(f"\n========================================")
print(f"Done: {ok} succeeded, {len(failed)} failed.")
if failed:
    print(f"Failed: {', '.join(failed)}")
