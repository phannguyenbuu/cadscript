; Phase 2: DXFOUT on the clean _ACAD.dwg file, output named without _ACAD suffix
(defun c:dxfout-clean ()
  (vl-load-com)
  (setvar "FILEDIA" 0)
  (setvar "CMDECHO" 1)
  (setq baseName (vl-filename-base (getvar "DWGNAME")))
  (setq dirName  (getvar "DWGPREFIX"))
  ; Strip _ACAD suffix: "B6_ACAD" -> "B6"
  (setq cleanBase
    (if (= (substr baseName (- (strlen baseName) 4) 5) "_ACAD")
      (substr baseName 1 (- (strlen baseName) 5))
      baseName
    )
  )
  (setq dxfFile (strcat dirName cleanBase ".dxf"))
  (princ (strcat "\n--- DXFOUT: " dxfFile " ---"))
  (command "_.DXFOUT" dxfFile "V" "2018" "16")
  (princ "\n--- DONE ---")
  (command "_.QUIT" "Y")
)
(c:dxfout-clean)
