# Checkliste für die Veröffentlichung

- [x] MIT-Lizenz für den eigenen Quellcode als `LICENSE` hinzugefügt.
- [x] `CHANGELOG.md` für Version 1.0.0 aktualisiert.
- [x] `.\build.ps1 -Test -Package` erfolgreich ausgeführt; Prüfsumme in den Release-Notizen ergänzt.
- [ ] Release-Archiv hochladen; keine VLC-Dateien, Logs, Verknüpfungen oder lokalen Einstellungen beilegen.
- [ ] Auf einem Surface Pro 8 bei 2880 × 1920 und 200 % Skalierung kurz prüfen.
- [ ] Falls der Release öffentlich ist: Code-Signing für künftige Versionen planen. Die EXE ist derzeit nicht signiert und kann daher von Windows SmartScreen abgefragt werden.
