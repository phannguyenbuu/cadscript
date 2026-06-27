import ezdxf
from ezdxf import recover
import os
import re

# Đường dẫn thư mục chứa file DXF
TARGET_DIR = r"D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details"

def process_all_dxfs():
    # Kiểm tra thư mục tồn tại
    if not os.path.exists(TARGET_DIR):
        print(f"Error: Directory not found: {TARGET_DIR}")
        return

    # Lấy danh sách file .dxf
    files = [f for f in os.listdir(TARGET_DIR) if f.lower().endswith('.dxf')]
    
    print(f"Scanning {len(files)} DXF files in: {TARGET_DIR}\n")
    
    for filename in files:
        # Bỏ qua nếu file đã có hậu tố _M trong tên (đã xử lý)
        if "_M" in filename:
            continue
            
        file_path = os.path.join(TARGET_DIR, filename)
        try:
            # Sử dụng chế độ recover để đọc file (chống lỗi missing ENDSEC)
            doc, auditor = recover.readfile(file_path)
            
            msp = doc.modelspace()
            m_value = None
            m_text_tag = ""
            
            # 1. Tìm text có dạng M20, M16...
            # Query cả TEXT (DbText) và MTEXT
            for entity in msp.query('TEXT MTEXT'):
                if entity.dxftype() == 'MTEXT':
                    content = entity.plain_text()
                else:
                    content = entity.dxf.text
                
                # Tìm chữ M followed by number
                match = re.search(r'M\s*(\d+(\.\d+)?)', content.strip(), re.IGNORECASE)
                if match:
                    m_value = float(match.group(1))
                    m_text_tag = f"M{match.group(1)}"
                    break
            
            if m_value is not None:
                # 2. Kiểm tra các Circle có đường kính khớp với m_value
                circles = msp.query('CIRCLE')
                # Kiểm tra xem có circle nào có Đường kính (radius * 2) = m_value không
                matched_circles = [c for c in circles if abs(c.dxf.radius * 2 - m_value) < 0.1]
                
                result_status = "MATCH" if matched_circles else "NO MATCH"
                print(f"File: {filename} -> Found {m_text_tag}. Circles {result_status}.")
                
                # 3. Đổi tên file (VD: A1.dxf -> A1_M20.dxf)
                base, ext = os.path.splitext(filename)
                new_filename = f"{base}_{m_text_tag}{ext}"
                new_path = os.path.join(TARGET_DIR, new_filename)
                
                # Thực hiện đổi tên
                try:
                    if os.path.exists(new_path):
                        os.remove(new_path)
                    os.rename(file_path, new_path)
                    print(f"   [OK] Renamed to: {new_filename}")
                except Exception as ren_err:
                    print(f"   [!] Rename failed: {ren_err}")
            else:
                print(f"File: {filename} -> No 'M*' tag found.")
                
        except Exception as e:
            # Nếu vẫn không đọc được dù dùng recover
            print(f"Error reading {filename}: {e}")

if __name__ == "__main__":
    process_all_dxfs()
