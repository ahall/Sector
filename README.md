# Sector
Sector is an easy to use database migration suite that uses FluentNHibernate. This is inspired by the sqlalchemy-migrate python library which I have missed coming from python.
## Contribution
This is a new library and all contribution is greatly appreciated.
## Preparation
First we need to create a repository which contains the SQL files we want to version for upgrade/downgrades. In this example we will put them into /tmp/repo. We start by issuing the following command:
    mkdir -p /tmp/repo /tmp/repo/versions

Next we edit /tmp/repo/sector.cfg and add the following text:
    [main]
    repository_id = Sector Test
Now lets create some versions, lets create /tmp/repo/versions/1_upgrade.sql and make it look like:
    CREATE TABLE testie(
        id serial PRIMARY KEY NOT NULL,        age integer NOT NULL UNIQUE,
        description varchar(255)
    );
And now /tmp/repo/versions/1_downgrade.sql
    DROP TABLE testie;

## Running the commands
In this example we are using PostgreSQL so we'll need to have the Npgsql driver in the current directory or in the GAC.
    $ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test migrate_ve
rsion    1
migrate_version only tells us what is the latest version available in the repository, it does not ask the database. Now lets do the db part and start by putt
ing our database under revision control.

    $ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test migrate_version_control
    $ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test migrate_db_version
    0

First we put the database under the revision control given the repository we created, after that's done the database is at version 0. Now lets upgrade. The upgrade command by default upgrades to the latest version, you can however pass it --version <ver> to change the version upgrading to.

    $ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test migrate_upgrade
    $ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test migrate_db_version
    1

Now we are going to downgrade back to 0

    $ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test --version 0 migrate_downgrade
    $ mono Sector.Tool.exe --repository-path=/tmp/repo --dbuser=ahall --dbpass=temp123 --dbhost=localhost --dbtype=postgresql --dbname=sector_test migrate_db_version
    0
