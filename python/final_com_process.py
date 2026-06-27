import win32com.client
import os
import re
import ezdxf
import time

# Cấu hình
TARGET_DIR = r"D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"

def safe_call(func, *args, retries=10, delay=2):
    """Hàm wrapper để gọi lệnh COM an toàn khi AutoCAD đang bận."""
    for i in range(retries):
        try:
            return func(*args)
        except Exception as e:
            if ("rejected by callee" in str(e).lower() or "busy" in str(e).lower()) and i < retries - 1:
                time.sleep(delay)
                continue
            raise e

def export_to_dxf_com():
    print("Step 1: Exporting DWG to DXF via AutoCAD COM (with Retry)...")
    
    try:
        acad = win32com.client.GetActiveObject("AutoCAD.Application")
    except:
        acad = win32com.client.Dispatch("AutoCAD.Application")
    
    acad.Visible = True
    dwgs = [f for f in os.listdir(TARGET_DIR) if f.lower().endswith('.dwg')]
    
    for dwg in dwgs:
        dwg_path = os.path.join(TARGET_DIR, dwg)
        dxf_path = os.path.join(TARGET_DIR, os.path.splitext(dwg)[0] + ".dxf")
        
        if os.path.exists(dxf_path):
            try: os.remove(dxf_path)
            except: pass
            
        print(f"  > Processing: {dwg}...", end=" ", flush=True)
        try:
            # Sử dụng safe_call cho các lệnh COM nhạy cảm
            doc = safe_call(acad.Documents.Open, dwg_path)
            safe_call(doc.SaveAs, dxf_path, 1) # R12 DXF
            
            # Đợi file ghi xong
            for _ in range(10):
                if os.path.exists(dxf_path) and os.path.getsize(dxf_path) > 1000:
                    break
                time.sleep(1)
                
            safe_call(doc.Close, False)
            print("Done.")
        except Exception as e:
            print(f"Error: {e}")

def process_dxf_logic():
    print("\nStep 2: Browsing DXF with Python...")
    dxf_files = [f for f in os.listdir(TARGET_DIR) if f.lower().endswith('.dxf')]
    
    for filename in dxf_files:
        if "_M" in filename: continue
        file_path = os.path.join(TARGET_DIR, filename)
        try:
            doc = ezdxf.readfile(file_path)
            msp = doc.modelspace()
            
            m_value = None
            m_tag = ""
            
            # Quét tìm M* trong ModelSpace và Blocks
            def find_m_tag(container):
                for t in container.query('TEXT MTEXT'):
                    content = t.plain_text() if t.dxftype() == 'MTEXT' else t.dxf.text
                    match = re.search(r'M\s*(\d+(\.\d+)?)', content.strip(), re.IGNORECASE)
                    if match:
                        return float(match.group(1)), f"M{match.group(1)}"
                return None, None

            m_value, m_tag = find_m_tag(msp)
            
            if not m_value:
                for block in doc.blocks:
                    if block.name.startswith('*'): continue
                    m_value, m_tag = find_m_tag(block)
                    if m_value: break

            if m_value:
                circles = msp.query('CIRCLE')
                matched = any(abs(c.dxf.radius * 2 - m_value) < 0.2 for c in circles)
                print(f"File: {filename} -> {m_tag}. Match Circle: {'YES' if matched else 'NO'}")
                
                base, ext = os.path.splitext(filename)
                new_name = f"{base}_{m_tag}{ext}"
                new_path = os.path.join(TARGET_DIR, new_name)
                
                if os.path.exists(new_path): os.remove(new_path)
                os.rename(file_path, new_path)
                print(f"   [OK] Renamed.")
            else:
                print(f"File: {filename} -> No 'M*' found.")
                
        except Exception as e:
            print(f"Error reading {filename}: {e}")

if __name__ == "__main__":
    export_to_dxf_com()
    time.sleep(2)
    process_dxf_logic()
