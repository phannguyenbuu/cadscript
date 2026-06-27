(defun c:conv-to-dxf (/ ssText txtContent ssCir diameter baseName newName cleanTxt)
  (vl-load-com)
  
  ; 1. Extract Text Content (First TEXT or MTEXT)
  (setq ssText (ssget "X" '((0 . "TEXT,MTEXT"))))
  (setq txtContent "")
  (if ssText
    (setq txtContent (cdr (assoc 1 (entget (ssname ssText 0)))))
  )
  
  ; 2. Extract Circle Diameter (First CIRCLE)
  (setq ssCir (ssget "X" '((0 . "CIRCLE"))))
  (setq diameter 0)
  (if ssCir
    (setq diameter (* 2 (cdr (assoc 40 (entget (ssname ssCir 0))))))
  )
  
  ; 3. Sanitize text content for filename
  (setq cleanTxt txtContent)
  ; Replace common invalid chars with underscore
  (setq cleanTxt (vl-string-translate " /\\:*?\"<>|" "__________" cleanTxt))
  
  ; 4. Construct new filename
  (setq baseName (vl-filename-base (getvar "DWGNAME")))
  (setq dirName (getvar "DWGPREFIX"))
  (setq newName (strcat dirName baseName))
  
  (if (/= cleanTxt "")
    (setq newName (strcat newName "_" cleanTxt))
  )
  (if (> diameter 0)
    (setq newName (strcat newName "_D" (rtos diameter 2 0)))
  )
  (setq newName (strcat newName ".dxf"))
  
  (princ (strcat "\nEXPORTING TO: " newName))
  
  ; 5. Run DXFOUT
  ; Version "V" "2007" for compatibility. Precision 16.
  (command "_.DXFOUT" newName "V" "2007" "16")
  
  (princ "\nSUCCESS!")
  ; Exit AutoCAD console
  (command "_.QUIT" "Y")
)
(c:conv-to-dxf)
