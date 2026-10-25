#!/usr/bin/perl
# Git clean filter for Unity assets (set up in CONTRIBUTING.md, Setup). Udon keeps each compiled
# program in the project's own Assets/SerializedUdonPrograms folder, under an id that differs in
# every project, and program assets and Udon behaviours reference it. The filter stages those
# references empty, so a compile in any project leaves git clean; Udon fills them in again.
use strict;
use warnings;
binmode STDIN;
binmode STDOUT;
local $/;
my $text = <STDIN> // '';
$text =~ s/(serializedUdonProgramAsset|serializedProgramAsset): \{fileID: \d+, guid: [0-9a-f]{32},\s*type: 2\}/$1: {fileID: 0}/g;
print $text;
