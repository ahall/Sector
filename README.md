Sector is an easy to use database migration suite that uses FluentNHibernate.

First we need to create a repository which contains the SQL files we want to version for upgrade/downgrades. In this example we will put them into /tmp/repo. We start by issuing the following command:
    mkdir -p /tmp/repo /tmp/repo/versions
Next we edit /tmp/repo/sector.cfg and add the following text:
    [main]
    repository_id = Sector Test
Now we create 
$ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test migrate_versio
n_control$ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test migrate_db_version
0
$ mono Sector.Tool.exe --repository-path=/Users/ahall/Projects/Sector/src/Sector.Tests/testfiles/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --db
type=postgresql --dbname=sector_test migrate_upgrade
