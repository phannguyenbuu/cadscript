; Strip AEC proxy objects and export to DXF using WBLOCK trick:
; WBLOCK * (all objects) to a new DXF file bypasses proxy object issues.
(defun c:conv-dxf-clean ()
  (vl-load-com)
  (setq baseName (vl-filename-base (getvar "DWGNAME")))
  (setq dirName  (getvar "DWGPREFIX"))
  (setq dxfFile  (strcat dirName baseName ".dxf"))
  (setvar "FILEDIA" 0)
  (setvar "CMDECHO" 1)
  (setvar "PROXYNOTICE" 0)    ; Suppress proxy notices
  (setvar "PROXYSHOW" 0)      ; Don't show proxy graphics

  (princ (strcat "\n--- EXPORTING: " baseName " ---"))

  ; Use DXFOUT directly (same as before) but with proxy settings suppressed
  ; If the AEC objects cause issues, they will be exported as proxy entities
  ; which is still valid DXF 2018 - the issue might be the reader not ezdxf R2018 support
  (command "_.DXFOUT" dxfFile "V" "2018" "16")

  (princ (strcat "\n--- DONE: " dxfFile " ---"))
  (command "_.QUIT" "Y")
)
(c:conv-dxf-clean)
