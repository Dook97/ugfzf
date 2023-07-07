                    _  _ __ _ / _|___/ _|
                   | || / _` |  _|_ /  _|
                    \_,_\__, |_| /__|_|
                        |___/

              ultimate-guitar.com fuzzy-finder
                by Dook97 (aka Jan Doskočil)

                        USER'S GUIDE

============================================================

                          [ABOUT]

This brief document serves as a guide to new users of the
ugfzf program.

ugfzf serves its user by allowing easy acces to the chords
and tabs found on ultimate-guitar.com from the comfort of
the terminal window, saving him the necessity of painful
interaction with modern web design.

                       [INSTALLATION]

You will need the following:
  * fzf in your PATH (github.com/junegunn/fzf)
  * dotnet runtime >=7.x
  * dotnet sdk >=7.x

Then simply clone this repo, cd into it and call

                       dotnet publish

A binary will appear in "./CLI/bin/Release/net7.x"

                          [USAGE]

Using the program is very straightforward. Invoke it with

                     ugfzf [your query]

After it is done loading the required data, a selection
window will appear. You can type to narrow down your options
or change your selection with the up and down keys. Select
the item you like by hitting enter. If you change your mind,
you can also quit without choosing anything - just hit Esc,
Ctrl-c or Ctrl-d.

The contents of your selection will then be printed to
stdout.

                      [COMPATIBILITY]

The program was tested on Linux and Windows 10. Macs will
most probably do just fine, but you should really find it in
you and use an OS for people with some notion of self-respect.

============================================================
github.com/Dook97/ugfzf
