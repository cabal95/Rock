#!/bin/sh
#

echo namespace Rock.Field.Types
echo {
for class in `grep -E ' class ([a-zA-Z]+) :' ../Field/Types/*.cs ../Field/SelectFromListFieldType.cs | sed -E 's/.* class ([a-zA-Z]+) :.*/\1/g'`; do
  echo "    public partial class $class : Rock.Field.FieldType { }"
done
echo }
