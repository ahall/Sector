CREATE TABLE testie(
    id serial PRIMARY KEY NOT NULL,
    age integer NOT NULL UNIQUE,
    description varchar(255)
);
