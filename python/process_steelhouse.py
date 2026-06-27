import win32com.client
import os
import re
import ezdxf
import time

# Cấu hình
TARGET_DIR = r"D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"

def export_to_dxf_stable():
    """Xuất DXF định dạng R12 để đảm bảo tính ổn định và tương thích cao nhất."""
    print("Step 1: Exporting to DXF R12 via COM...")
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
            
        print(f"  Exporting: {dwg}...", end=" ", flush=True)
        try:
            doc = acad.Documents.Open(dwg_path)
            # Sử dụng định dạng 1 (AutoCAD R12 DXF) - Rất nhẹ và ổn định
            doc.SaveAs(dxf_path, 1) 
            # Đợi một chút để AutoCAD hoàn tất ghi file
            time.sleep(1)
            doc.Close(False)
            print("OK")
        except Exception as e:
            print(f"FAILED: {e}")

def process_with_python():
    """Duyệt file DXF bằng Python để xử lý logic M* và Circle."""
    print("\nStep 2: Browsing DXF with Python...")
    dxf_files = [f for f in os.listdir(TARGET_DIR) if f.lower().endswith('.dxf')]
    
    for filename in dxf_files:
        if "_M" in filename: continue
        file_path = os.path.join(TARGET_DIR, filename)
        try:
            # Đọc DXF
            doc = ezdxf.readfile(file_path)
            msp = doc.modelspace()
            
            m_value = None
            m_tag = ""
            
            # Tìm text M*
            for t in msp.query('TEXT MTEXT'):
                content = t.plain_text() if t.dxftype() == 'MTEXT' else t.dxf.text
                match = re.search(r'M\s*(\d+(\.\d+)?)', content.strip(), re.IGNORECASE)
                if match:
                    m_value = float(match.group(1))
                    m_tag = f"M{match.group(1)}"
                    break
            
            if m_value:
                # Kiểm tra Circle
                circles = msp.query('CIRCLE')
                # Check đường kính (Radius * 2)
                matched = any(abs(c.dxf.radius * 2 - m_value) < 0.1 for c in circles)
                
                print(f"File: {filename} -> {m_tag} | Match Circle: {'YES' if matched else 'NO'}")
                
                # Đổi tên file
                base, ext = os.path.splitext(filename)
                new_name = f"{base}_{m_tag}{ext}"
                new_path = os.path.join(TARGET_DIR, new_name)
                
                if os.path.exists(new_path): os.remove(new_path)
                os.rename(file_path, new_path)
                print(f"   [OK] Renamed.")
            else:
                print(f"File: {filename} -> No 'M*' tag found.")
                
        except Exception as e:
            print(f"Error reading {filename}: {e}")

if __name__ == "__main__":
    export_to_dxf_stable()
    time.sleep(2)
    process_with_python()
