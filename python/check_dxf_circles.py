import ezdxf
import os
import re

def process_dxf_files(folder_path):
    # Get list of DXF files
    files = [f for f in os.listdir(folder_path) if f.lower().endswith('.dxf')]
    
    print(f"Scanning {len(files)} DXF files in: {folder_path}\n")
    
    for filename in files:
        # Skip if already renamed
        if "_M" in filename:
            continue
            
        file_path = os.path.join(folder_path, filename)
        try:
            doc = ezdxf.readfile(file_path)
            msp = doc.modelspace()
            
            m_value = None
            m_text_found = ""
            
            # 1. Search for Text/MText starting with 'M'
            for entity in msp.query('TEXT MTEXT'):
                if entity.dxftype() == 'MTEXT':
                    # Use plain_text() to strip formatting codes like \P, \f, etc.
                    content = entity.plain_text()
                else:
                    content = entity.dxf.text
                
                content = content.strip()
                # Regex for M followed by number (e.g. M20, M 20, M12.5)
                match = re.search(r'M\s*(\d+(\.\d+)?)', content, re.IGNORECASE)
                if match:
                    m_value = float(match.group(1))
                    m_text_found = f"M{match.group(1)}"
                    break
            
            if m_value is not None:
                # 2. Check Circles
                circles = msp.query('CIRCLE')
                match_count = 0
                
                for circle in circles:
                    diameter = circle.dxf.radius * 2
                    # The user said "M means radius" but check "diameter equals value"
                    # I will check if diameter matches M_value
                    if abs(diameter - m_value) < 0.1:
                        match_count += 1
                
                status = f"Matches: {match_count} circle(s)" if match_count > 0 else "NO MATCH"
                print(f"File: {filename} -> Found {m_text_found}. {status}")
                
                # 3. Rename file
                base, ext = os.path.splitext(filename)
                new_filename = f"{base}_{m_text_found}{ext}"
                new_path = os.path.join(folder_path, new_filename)
                
                try:
                    os.rename(file_path, new_path)
                    print(f"   [OK] Renamed to: {new_filename}")
                except Exception as rename_err:
                    print(f"   [!] Rename failed: {rename_err}")
            else:
                print(f"File: {filename} -> No 'M*' text found.")
                
        except Exception as e:
            print(f"Error processing {filename}: {e}")

if __name__ == "__main__":
    # Folder path from request
    target_dir = r'D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details'
    process_dxf_files(target_dir)
