ShopBackend
Setup & Readme

Projekt:	ShopBackend (ASP.NET Core + EF Core)
Technologie:	C# / .NET / SQL Server / JWT / Bcrypt / HTML / JS / CSS
Architektur:	4-Schicht: API / Application / Domain / Infrastructure 

Frontend in HTML

Zielgruppe:		      Personen, die das Projekt frisch von Git klonen.

Voraussetzungen:	      .NET 8 SDK, SQL Server LocalDB, Visual Studio.

API Ports:		     (HTTP): http://localhost:5139 
     Swagger URL: http://localhost:5139 

	 

1.	Einrichtung – Schritt für Schritt
Diese Anleitung beschreibt die exakten Schritte nach dem Git-Clone, um das Projekt lauffähig zu machen. Die häufigsten Stolperstellen sind der HTTPS-Port und die Datenbankverbindung.


1.1 Voraussetzungen installieren
•	.NET 8 SDK: Verfügbar unter https://dotnet.microsoft.com/download
•	SQL Server Express oder LocalDB: Wird mit Visual Studio mitgeliefert oder separat unter https://www.microsoft.com/sql-server
•	Git: Verfügbar unter https://git-scm.com
•	IDE: Visual Studio 2022 (Community) 

Wichtiger Hinweis: LocalDB ist der Standard in der appsettings.json. 
Bei Nutzung eines vollwertigen SQL Servers muss der Connection String angepasst werden.
Das Projekt verwendet außerdem eine ältere Version von swashbuckle.AspNetCore für die SwaggerUi. Hier meckert VS zwar, allerdings ist dies nur ein lokales Fachprojekt. 
Installiert man eine aktuellere Version, crasht es mit denen von .Net und Microsoft API im Bezug auf neue mir unbekannte Namespaces.


1.2 Repository klonen & öffnen
Führe die folgenden Befehle in deinem Terminal aus:
Bash
git clone https://github.com/<dein-repo>/ShopBackend.git
cd ShopBackend

Öffne danach die Solution ShopBackend.sln in Visual Studio 




1.3 appsettings.json prüfen & anpassen
Die Konfigurationsdatei liegt unter ShopBackend.API/appsettings.json. Der Standardinhalt sieht so aus:
JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ShopBackend;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "mein-sehr-langer-geheimer-schluessel-min-32-zeichen",
    "Issuer": "ShopBackend",
    "Audience": "ShopFrontend"
  }
}
Sicherheitshinweis: Den JWT Key sollte man unbedingt ändern, wenn das Projekt öffentlich läuft. Er muss mindestens 32 Zeichen lang und möglichst zufällig sein. Wer einen echten SQL Server nutzt, passt die DefaultConnection entsprechend an 
(z.B. Server=localhost;Database=ShopBackend;User Id=sa;Password=DeinPasswort;TrustServerCertificate=True;).


1.4 Datenbank erstellen (Migration)
Die EF Core-Migrationen sind bereits vorhanden. Lege die Datenbank mit folgendem Befehl an:
Bash
cd ShopBackend.API
dotnet ef database update
(Alternativ in der Package Manager Console in Visual Studio: Update-Database)
Erfolgskontrolle: Beim ersten Start legt das Projekt automatisch Seed-Daten an (Admin, Staff, zwei Kunden, Produkte, eine Beispielbestellung). Es ist kein manuelles SQL-Skript nötig.

1.5 Projekt starten & Port-Konfiguration
Starte das Projekt über das HTTP-Profil (empfohlen für lokales Testen):
Bash
cd ShopBackend.API
dotnet run --launch-profile http
Das Frontend ist nun unter http://localhost:5139 erreichbar. 
Swagger unter http://localhost:5139/swagger.

Das HTTPS-Port Problem: 
Der HTTPS-Port 7131 ist NUR auf der ursprünglichen Entwicklungsmaschine gültig, da Visual Studio diesen zufällig vergibt. Nutze lokal immer den HTTP-Port 5139. Das Frontend nutzt relative Pfade (/api), was nur funktioniert, wenn man über den eingebauten Static-File-Server (localhost:5139) zugreift.



1.6 Seed-Daten – Standard-Zugangsdaten
E-Mail	Passwort	Rolle	Hinweis
admin@shop.de	Admin123!	Admin	Voller Zugriff, ID=1, unlöschbar
staff@shop.de	Staff123!	Staff	Zugriff auf Produkte, Bestellungen, Rechnungen (Frontend nur minimal verfügbar)
max.mustermann@mail.de	Kunde123!	Customer	Leeres Profil zum freien Testen
anna.schmidt@mail.de	Kunde123!	Customer	Hat bereits eine Bestellung + Rechnung 








1.7 Häufige Fehler beim ersten Start

•	“No connection could be made” / DB-Fehler: SQL Server LocalDB läuft nicht. Starte ihn über die Konsole (sqllocaldb start MSSQLLocalDB) oder den SQL Server Object Explorer in Visual Studio.
•	Migration fehlt / Tabellen nicht vorhanden: Führe dotnet ef database update erneut aus oder setze die DB zurück (dotnet ef database drop & dotnet ef database update).
•	401 Unauthorized auf allen Endpoints: Der JWT-Key, Issuer oder Audience in der appsettings.json stimmt nicht überein.
•	Frontend lädt, aber Login schlägt fehl (CORS): Die HTML-Dateien wurden direkt im Browser oder über eine Live Server Extension geöffnet. Rufe sie zwingend über http://localhost:5139 auf.
•	Zirkuläre JSON-Fehler: In der Program.cs fehlt ReferenceHandler.IgnoreCycles in den JsonOptions.




















Schnell-Referenzen
URL	Beschreibung
http://localhost:5139 
Frontend (Startseite / Login)
http://localhost:5139/swagger 
Swagger UI (nur im Development-Modus)
http://localhost:5139/api/auth/login 
Login-Endpoint (POST)
http://localhost:5139/Admin.html 
Direkter Aufruf des Admin-Dashboards
http://localhost:5139/Shop.html 
Direkter Aufruf des Kunden-Shops

