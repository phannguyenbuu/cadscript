(defun c:conv-dxf ()
  (vl-load-com)
  (setq baseName (vl-filename-base (getvar "DWGNAME")))
  (setq newName (strcat (getvar "DWGPREFIX") baseName ".dxf"))
  (setvar "FILEDIA" 0)
  (setvar "CMDECHO" 1)

  (princ (strcat "\n--- STARTING EXPORT: " baseName " ---"))

  ; DXFOUT prompts: filename -> [precision / V / B / O / P] -> version -> precision
  ; Pass "V" then "2013" (no "R" prefix) then precision "16"
  (command "_.DXFOUT" newName "V" "2018" "16")

  (princ "\n--- EXPORT FINISHED ---")
  (command "_.QUIT" "Y")
)
(c:conv-dxf)
