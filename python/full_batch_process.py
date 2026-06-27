import subprocess
import os
import ezdxf
from ezdxf import recover
import re
import time

# Configurations
CONSOLE = r"C:\Program Files\Autodesk\AutoCAD 2022\accoreconsole.exe"
TARGET_DIR = r"D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"
TEMP_SCR = os.path.join(os.environ.get('TEMP', 'C:\\Temp'), "batch_gen.scr")

def generate_dxf_files():
    """Export DWG to DXF using AccoreConsole."""
    dwg_files = [f for f in os.listdir(TARGET_DIR) if f.lower().endswith('.dwg')]
    print(f"Starting export of {len(dwg_files)} DWG files to DXF...")
    
    for dwg in dwg_files:
        dwg_path = os.path.join(TARGET_DIR, dwg)
        dxf_path = os.path.join(TARGET_DIR, os.path.splitext(dwg)[0] + ".dxf")
        
        # Temp script
        with open(TEMP_SCR, 'w') as f:
            f.write(f'._DXFOUT "{dxf_path}" \n')
            f.write('._QUIT _Y\n')
            
        cmd = f'"{CONSOLE}" /i "{dwg_path}" /s "{TEMP_SCR}"'
        print(f"  > Exporting: {dwg}...", end=" ", flush=True)
        
        try:
            subprocess.run(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            time.sleep(2)
            if os.path.exists(dxf_path) and os.path.getsize(dxf_path) > 5000:
                print("Done.")
            else:
                print("Error (Empty or missing).")
        except Exception as e:
            print(f"Error: {e}")

def process_dxf_logic():
    """Browse DXF with Python and rename."""
    print("\nProcessing DXF content with Python...")
    dxf_files = [f for f in os.listdir(TARGET_DIR) if f.lower().endswith('.dxf')]
    
    for filename in dxf_files:
        if "_M" in filename: continue
        file_path = os.path.join(TARGET_DIR, filename)
        try:
            doc, auditor = recover.readfile(file_path)
            msp = doc.modelspace()
            
            m_value = None
            m_tag = ""
            
            for t in msp.query('TEXT MTEXT'):
                content = t.plain_text() if t.dxftype() == 'MTEXT' else t.dxf.text
                match = re.search(r'M\s*(\d+(\.\d+)?)', content.strip(), re.IGNORECASE)
                if match:
                    m_value = float(match.group(1))
                    m_tag = f"M{match.group(1)}"
                    break
            
            if m_value:
                circles = msp.query('CIRCLE')
                # Check diameter (radius * 2)
                matched = any(abs(c.dxf.radius * 2 - m_value) < 0.2 for c in circles)
                
                status = "MATCH" if matched else "NO MATCH"
                print(f"File: {filename} -> {m_tag}. Circle: {status}")
                
                base, ext = os.path.splitext(filename)
                new_name = f"{base}_{m_tag}{ext}"
                new_path = os.path.join(TARGET_DIR, new_name)
                
                if os.path.exists(new_path): os.remove(new_path)
                os.rename(file_path, new_path)
                print(f"   [OK] Renamed to: {new_name}")
            else:
                print(f"File: {filename} -> No 'M*' found.")
                
        except Exception as e:
            print(f"Error {filename}: {e}")

if __name__ == "__main__":
    generate_dxf_files()
    time.sleep(2)
    process_dxf_logic()
