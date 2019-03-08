#!/bin/sh
#

echo 'namespace Rock.Migrations.Migrations {'
echo '    public partial class RockMigrationSQL {'
IFS=$'\n'
for path in `grep 'Content Include=' ../Rock.Migrations.Core.csproj | sed -E 's/.*="(.*)".*/\1/g'`; do
  file=${path##*\\}
  property=`echo $file | cut -f1 -d. | sed 's/[ \-]/_/g'`
  ext=`echo $file | cut -f2 -d.`
  if [ "$ext" == "gz" ]; then
    echo '        public static byte[] _'$property' => GetBinaryResource( @"'$path'" );'
  else
    echo '        public static string _'$property' => GetSqlResource( @"'$path'" );'
  fi
  echo ''
done
echo '    }'
echo '}'

