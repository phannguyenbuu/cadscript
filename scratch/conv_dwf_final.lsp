(defun c:conv-dwf ()
  (vl-load-com)
  (setq baseName (vl-filename-base (getvar "DWGNAME")))
  (setq newName (strcat (getvar "DWGPREFIX") baseName ".dwf"))
  (setvar "FILEDIA" 0)
  (setvar "CMDECHO" 1)
  
  (princ (strcat "\n--- STARTING EXPORT: " baseName " ---"))
  
  ; Attempt EXPORT
  (command "_.EXPORT" newName)
  
  ; Loop to handle all possible prompts (All, Materials, etc.)
  (while (> (getvar "CMDACTIVE") 0)
    (command "") ; Send Enter for defaults (usually All and Yes)
  )
  
  (princ "\n--- EXPORT FINISHED ---")
  (command "_.QUIT" "Y")
)
(c:conv-dwf)
