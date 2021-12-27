# murmur3Hash_dumper
simple command line tool used in my research for halo infinite.
basically my weird take on trying to brute force reversing hashes

essentially what this tool tries to achieve
- reverse murmur3 hashes by stripping all the keywords/strings from selected files, hash them, and then assort them into a large database

utilites (all load the hashed strings ontop of the currently loaded ones)
- load previous dump - will load a previously exported hashed string database
- hash strings from directory - will dig through every file in directory for strings to hash and add to list of currently hashed strings
- dump loaded hashed strings - export the hashed strings that are currently loaded into a text file database 
- filter against directory - attempt to narrow down list of currently loaded hashed strings by looking for references to the hashes themselves within all files in a directory

'hash strings from directory' and 'filter against directory' are really slow lol, i have yet to optimize these processes

murmur3 code derived from https://gist.github.com/automatonic/3725443
