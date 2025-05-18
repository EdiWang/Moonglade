export default function (hljs) {
    return {
        name: 'Bicep',
        keywords: {
            keyword:
                'resource module param var output targetScope for in if existing import',
            type:
                'string int bool object array any',
            literal:
                'true false null'
        },
        contains: [
            hljs.COMMENT('//', '$'),
            hljs.COMMENT('/\\*', '\\*/'),
            {
                className: 'string',
                variants: [
                    hljs.QUOTE_STRING_MODE,
                    hljs.APOS_STRING_MODE
                ]
            },
            {
                className: 'number',
                begin: '\\b\\d+(\\.\\d+)?\\b'
            }
        ]
    };
}
