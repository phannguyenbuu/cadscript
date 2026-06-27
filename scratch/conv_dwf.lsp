(defun c:conv-dwf ()
  (vl-load-com)
  (setq baseName (vl-filename-base (getvar "DWGNAME")))
  (setq dirName (getvar "DWGPREFIX"))
  (setq newName (strcat dirName baseName ".dwf"))
  
  (princ (strcat "\n--- PROCESSING: " baseName " ---"))
  
  ; Set file dialogs off
  (setvar "FILEDIA" 0)
  (setvar "CMDECHO" 1)
  
  ; Try 3DDWF first as it's often more console-friendly for DWF export
  ; It works for 2D drawings too.
  (if (member "3DDWF" (atoms-family 1))
    (progn
      (princ "\nUsing 3DDWF command...")
      (command "_.3DDWF" newName)
    )
    (progn
      (princ "\nUsing EXPORT command...")
      ; EXPORT DWF [Filename] [All]
      (command "_.EXPORT" "DWF" newName)
    )
  )
  
  (princ "\n--- FINISHED ---")
  (command "_.QUIT" "Y")
)
(c:conv-dwf)
