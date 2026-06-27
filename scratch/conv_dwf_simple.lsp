(defun c:conv-dwf ()
  (vl-load-com)
  (setq newName (strcat (getvar "DWGPREFIX") (vl-filename-base (getvar "DWGNAME")) ".dwf"))
  (setvar "FILEDIA" 0)
  (setvar "CMDECHO" 1)
  (princ (strcat "\nExporting to: " newName))
  
  ; Run EXPORT and provide the filename. Extension .dwf tells it what format to use.
  (command "_.EXPORT" newName)
  
  ; Handle potential selection prompt
  (while (> (getvar "CMDACTIVE") 0)
    (command "ALL" "")
  )
  
  (princ "\nDone!")
  (command "_.QUIT" "Y")
)
(c:conv-dwf)
