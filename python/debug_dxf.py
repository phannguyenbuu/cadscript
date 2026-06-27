import ezdxf
from ezdxf import recover
import os

file_path = r"D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details\A1.dxf"

def debug_dxf():
    try:
        doc, auditor = recover.readfile(file_path)
        msp = doc.modelspace()
        print(f"Entities found: {len(msp)}")
        
        texts = msp.query('TEXT MTEXT')
        print(f"Text entities: {len(texts)}")
        
        for i, t in enumerate(texts):
            content = t.plain_text() if t.dxftype() == 'MTEXT' else t.dxf.text
            print(f"[{i}] {content}")
            if i > 20: break
            
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    debug_dxf()
