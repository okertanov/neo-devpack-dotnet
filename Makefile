##
## Copyright (c) 2022 - Team11. All rights reserved.
##

all:
	make -C src/Neo.Compiler.CSharp $@

build:
	make -C src/Neo.Compiler.CSharp $@

restore:
	make -C src/Neo.Compiler.CSharp $@

test:
	make -C src/Neo.Compiler.CSharp $@

align-project:
	make -C src/Neo.Compiler.CSharp $@

clean:
	make -C src/Neo.Compiler.CSharp $@

.PHONY: all build restore align-project test clean

.SILENT: clean
