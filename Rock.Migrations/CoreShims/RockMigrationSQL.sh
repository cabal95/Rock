#!/bin/sh
#

echo 'namespace Rock.Migrations.Migrations {'
echo '    public partial class RockMigrationSQL {'
IFS=$'\n'
for path in `grep 'EmbeddedResource Include=' ../Rock.Migrations.Core.csproj | grep -v '\.resx' | sed -E 's/.*="(.*)".*/\1/g'`; do
  file=${path##*\\}
  property=`echo $file | cut -f1 -d. | sed 's/[ \-]/_/g'`
  ext=`echo $file | cut -f2 -d.`
  respath=`echo "$path" | sed -E 's/Version ([0-9]+)\\.([0-9]+)\\.([0-9]+)/Version_\\1._\\2._\\3/g' | sed -E 's/Version ([0-9]+)\\.([0-9]+)/Version_\\1._\\2/g' | sed 's/\\\\/./g'`
  if [ "$ext" == "gz" ]; then
    echo '        public static byte[] _'$property' => GetBinaryResource( @"Rock.Migrations.'$respath'" );'
  else
    echo '        public static string _'$property' => GetSqlResource( @"Rock.Migrations.'$respath'" );'
  fi
  echo ''
done
echo '    }'
echo '}'

