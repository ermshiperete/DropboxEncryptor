# DropboxEncryptor

This project is abandoned because it's design has to many problems - see below.

## Background

The original idea behind this project was to have an encrypted and an decrypted directory and
two file system watchers. If a change happens in either directory the changed file gets
de-/encrypted.

While this sounded like a good idea it has some problems:
The file system watchers call an event handler when a change happens. The event handlers are asynchron.
I implemented a queue to serialize the processing. However, this causes queued events to pile up.
If a file gets deleted in the decrypted directory and later recreated, the processing of the
deleted-event (which deletes the encrypted file) might happen after the decrypted file got
recreated. The deletion of the encrypted file causes another decrypted-file-deleted event
which when processed deletes the decrypted file. Later on when we process the decrypted-file-created event
there no longer is a decrypted file, resulting in the loss of the file.

While a fix for this could be implemented (e.g. by creating a lock file that tells the
direction), I realized that it has inherent problems: creating the lock file and making
the change to the de-/encrypted file is not atomic, so there's always the danger that the
event occurs after creating the lock file but before changing the de-/encrypted file (or
vice versa if the order is switched). Therefore I decided to abandon this project.

## Alternative

After abandoning this project I went back to the planning board, and this time I
discovered that there is actually a project that would fit my needs (per file
encryption without encryption of the file names so that the naming of conflicting
files on Dropbox remains the same): [gocryptfs](https://nuetzlich.net/gocryptfs/)
has a `-plaintextnames` option.