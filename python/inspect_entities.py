import ezdxf
from ezdxf import recover
import os

file_path = r"D:\Dropbox\_Documents\_Vlance_2026\SteelHouse\details\A1.dxf"

def list_all_texts():
    try:
        doc, auditor = recover.readfile(file_path)
        msp = doc.modelspace()
        
        print(f"File: {file_path}")
        print(f"Total entities in ModelSpace: {len(msp)}")
        
        # Count types
        types = {}
        for e in msp:
            t = e.dxftype()
            types[t] = types.get(t, 0) + 1
        print(f"Entity types: {types}")
        
        # Print first 50 text contents from anywhere
        print("\nSearching all TEXT/MTEXT in ModelSpace:")
        for e in msp.query('TEXT MTEXT'):
            content = e.plain_text() if e.dxftype() == 'MTEXT' else e.dxf.text
            print(f"  - {content}")
            
        # Search inside BLOCKS
        print("\nSearching inside BLOCKS:")
        for block in doc.blocks:
            if block.name.startswith('*'): continue # Skip internal blocks
            for e in block.query('TEXT MTEXT'):
                content = e.plain_text() if e.dxftype() == 'MTEXT' else e.dxf.text
                print(f"  [Block {block.name}] - {content}")
                
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    list_all_texts()
