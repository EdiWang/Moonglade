$sourcePath = ".\min"
$destinationPath = ".\moonglade-monaco\"

if (-Not (Test-Path -Path $destinationPath)) {
    New-Item -ItemType Directory -Path $destinationPath -Force
}

Copy-Item -Path $sourcePath -Destination $destinationPath -Recurse -Force

$workerFilesToDelete = @(
    ".\moonglade-monaco\min\vs\base\common\worker\simpleWorker.nls.de.js",
    ".\moonglade-monaco\min\vs\base\common\worker\simpleWorker.nls.es.js",
    ".\moonglade-monaco\min\vs\base\common\worker\simpleWorker.nls.fr.js",
    ".\moonglade-monaco\min\vs\base\common\worker\simpleWorker.nls.it.js",
    ".\moonglade-monaco\min\vs\base\common\worker\simpleWorker.nls.ja.js",
    ".\moonglade-monaco\min\vs\base\common\worker\simpleWorker.nls.ko.js",
    ".\moonglade-monaco\min\vs\base\common\worker\simpleWorker.nls.ru.js"
)

foreach ($file in $workerFilesToDelete) {
    if (Test-Path -Path $file) {
        Remove-Item -Path $file -Force
    }
}

$basicLanguagesFoldersToDelete = @(
    "abap", "apex", "azcli", "bat", "bicep", "cameligo", "clojure", "coffee", "cpp", "csharp", 
    "csp", "cypher", "dart", "dockerfile", "ecl", "elixir", "flow9", "freemarker2", "fsharp", 
    "go", "graphql", "handlebars", "hcl", "ini", "java", "julia", "kotlin", "less", "lexon", 
    "liquid", "lua", "m3", "mdx", "mips", "msdax", "mysql", "objective-c", "pascal", "pascaligo", 
    "perl", "pgsql", "php", "pla", "postiats", "powerquery", "powershell", "protobuf", "pug", 
    "python", "qsharp", "r", "razor", "redis", "redshift", "restructuredtext", "ruby", "rust", 
    "sb", "scala", "scheme", "scss", "shell", "solidity", "sophia", "sparql", "sql", "st", 
    "swift", "systemverilog", "tcl", "twig", "typescript", "typespec", "vb", "wgsl", "yaml"
)

foreach ($folder in $basicLanguagesFoldersToDelete) {
    $folderPath = ".\moonglade-monaco\min\vs\basic-languages\$folder"
    if (Test-Path -Path $folderPath) {
        Remove-Item -Path $folderPath -Recurse -Force
    }
}

$editorFilesToDelete = @(
    ".\moonglade-monaco\min\vs\editor\editor.main.nls.de.js",
    ".\moonglade-monaco\min\vs\editor\editor.main.nls.es.js",
    ".\moonglade-monaco\min\vs\editor\editor.main.nls.fr.js",
    ".\moonglade-monaco\min\vs\editor\editor.main.nls.it.js",
    ".\moonglade-monaco\min\vs\editor\editor.main.nls.ja.js",
    ".\moonglade-monaco\min\vs\editor\editor.main.nls.ko.js",
    ".\moonglade-monaco\min\vs\editor\editor.main.nls.ru.js"
)

foreach ($file in $editorFilesToDelete) {
    if (Test-Path -Path $file) {
        Remove-Item -Path $file -Force
    }
}
