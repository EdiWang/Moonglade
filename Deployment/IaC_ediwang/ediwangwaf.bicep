param wafname string = 'ediwangwaf'

resource wafname_resource 'Microsoft.Network/frontdoorwebapplicationfirewallpolicies@2025-03-01' = {
  name: wafname
  location: 'Global'
  sku: {
    name: 'Premium_AzureFrontDoor'
  }
  properties: {
    policySettings: {
      enabledState: 'Enabled'
      mode: 'Prevention'
      redirectUrl: 'https://996.icu'
      customBlockResponseStatusCode: 403
      customBlockResponseBody: 'WW91IGFyZSBibG9jayBieSBBenVyZSBXZWIgQXBwbGljYXRpb24gRmlyZXdhbGwgZHVlIHRvIHN1c3BpY2lvdXMgYWN0aXZpdGllcyBvciB2aXNpdGluZyBmcm9tIGJsb2NrZWQgcmVnaW9ucy4='
      requestBodyCheck: 'Disabled'
      javascriptChallengeExpirationInMinutes: 30
      captchaExpirationInMinutes: 30
    }
    customRules: {
      rules: [
        {
          name: 'NoRussia'
          enabledState: 'Enabled'
          priority: 10
          ruleType: 'MatchRule'
          rateLimitDurationInMinutes: 1
          rateLimitThreshold: 100
          matchConditions: [
            {
              matchVariable: 'SocketAddr'
              operator: 'GeoMatch'
              negateCondition: false
              matchValue: [
                'RU'
              ]
              transforms: []
            }
          ]
          action: 'Block'
          groupBy: []
        }
        {
          name: 'NoThailand'
          enabledState: 'Enabled'
          priority: 11
          ruleType: 'MatchRule'
          rateLimitDurationInMinutes: 1
          rateLimitThreshold: 100
          matchConditions: [
            {
              matchVariable: 'SocketAddr'
              operator: 'GeoMatch'
              negateCondition: false
              matchValue: [
                'TH'
              ]
              transforms: []
            }
          ]
          action: 'Block'
          groupBy: []
        }
        {
          name: 'AllowAdmin'
          enabledState: 'Enabled'
          priority: 20
          ruleType: 'MatchRule'
          rateLimitDurationInMinutes: 1
          rateLimitThreshold: 100
          matchConditions: [
            {
              matchVariable: 'Cookies'
              selector: '.AspNetCore.Cookies'
              operator: 'Any'
              negateCondition: false
              matchValue: []
              transforms: []
            }
          ]
          action: 'Allow'
          groupBy: []
        }
        {
          name: 'BlockHackingExtensions'
          enabledState: 'Enabled'
          priority: 30
          ruleType: 'MatchRule'
          rateLimitDurationInMinutes: 0
          rateLimitThreshold: 0
          matchConditions: [
            {
              matchVariable: 'RequestUri'
              operator: 'EndsWith'
              negateCondition: false
              matchValue: [
                '.7z'
                '.ashx'
                '.asmx'
                '.asp'
                '.aspx'
                '.gz'
                '.jsp'
                '.php'
                '.tar'
                '.zip'
              ]
              transforms: [
                'Lowercase'
              ]
            }
          ]
          action: 'Block'
          groupBy: []
        }
        {
          name: 'RateLimitComment'
          enabledState: 'Enabled'
          priority: 40
          ruleType: 'RateLimitRule'
          rateLimitDurationInMinutes: 1
          rateLimitThreshold: 30
          matchConditions: [
            {
              matchVariable: 'RequestUri'
              operator: 'Contains'
              negateCondition: false
              matchValue: [
                '/api/comment'
              ]
              transforms: [
                'Lowercase'
                'Trim'
              ]
            }
            {
              matchVariable: 'RequestMethod'
              operator: 'Equal'
              negateCondition: false
              matchValue: [
                'POST'
              ]
              transforms: []
            }
          ]
          action: 'Block'
          groupBy: [
            {
              variableName: 'SocketAddr'
            }
          ]
        }
        {
          name: 'RateLimitPost'
          enabledState: 'Enabled'
          priority: 41
          ruleType: 'RateLimitRule'
          rateLimitDurationInMinutes: 1
          rateLimitThreshold: 300
          matchConditions: [
            {
              matchVariable: 'RequestUri'
              operator: 'Contains'
              negateCondition: false
              matchValue: [
                '/post/'
              ]
              transforms: [
                'Lowercase'
                'Trim'
              ]
            }
          ]
          action: 'Block'
          groupBy: [
            {
              variableName: 'SocketAddr'
            }
          ]
        }
      ]
    }
    managedRules: {
      managedRuleSets: [
        {
          ruleSetType: 'DefaultRuleSet'
          ruleSetVersion: '1.0'
          ruleGroupOverrides: [
            {
              ruleGroupName: 'SQLI'
              rules: [
                {
                  ruleId: '942440'
                  enabledState: 'Disabled'
                  action: 'Block'
                  exclusions: [
                    {
                      matchVariable: 'RequestCookieNames'
                      selectorMatchOperator: 'Contains'
                      selector: '.AspNetCore.Cookies'
                    }
                    {
                      matchVariable: 'RequestCookieNames'
                      selectorMatchOperator: 'Contains'
                      selector: 'X-CSRF-TOKEN-MOONGLADE'
                    }
                  ]
                }
                {
                  ruleId: '942450'
                  enabledState: 'Disabled'
                  action: 'Block'
                  exclusions: []
                }
                {
                  ruleId: '942430'
                  enabledState: 'Enabled'
                  action: 'Block'
                  exclusions: [
                    {
                      matchVariable: 'RequestBodyPostArgNames'
                      selectorMatchOperator: 'Contains'
                      selector: 'EditorContent'
                    }
                  ]
                }
                {
                  ruleId: '942260'
                  enabledState: 'Enabled'
                  action: 'Block'
                  exclusions: [
                    {
                      matchVariable: 'RequestBodyPostArgNames'
                      selectorMatchOperator: 'Contains'
                      selector: 'EditorContent'
                    }
                  ]
                }
                {
                  ruleId: '942370'
                  enabledState: 'Enabled'
                  action: 'Block'
                  exclusions: [
                    {
                      matchVariable: 'RequestBodyPostArgNames'
                      selectorMatchOperator: 'Contains'
                      selector: 'EditorContent'
                    }
                  ]
                }
                {
                  ruleId: '942200'
                  enabledState: 'Enabled'
                  action: 'Block'
                  exclusions: [
                    {
                      matchVariable: 'RequestBodyPostArgNames'
                      selectorMatchOperator: 'Contains'
                      selector: 'EditorContent'
                    }
                  ]
                }
              ]
              exclusions: []
            }
            {
              ruleGroupName: 'XSS'
              rules: [
                {
                  ruleId: '941320'
                  enabledState: 'Disabled'
                  action: 'Block'
                  exclusions: []
                }
                {
                  ruleId: '941150'
                  enabledState: 'Enabled'
                  action: 'Block'
                  exclusions: [
                    {
                      matchVariable: 'RequestBodyPostArgNames'
                      selectorMatchOperator: 'Contains'
                      selector: 'EditorContent'
                    }
                  ]
                }
                {
                  ruleId: '941340'
                  enabledState: 'Enabled'
                  action: 'Block'
                  exclusions: [
                    {
                      matchVariable: 'RequestBodyPostArgNames'
                      selectorMatchOperator: 'Contains'
                      selector: 'EditorContent'
                    }
                  ]
                }
              ]
              exclusions: []
            }
            {
              ruleGroupName: 'MS-ThreatIntel-CVEs'
              rules: [
                {
                  ruleId: '99001016'
                  enabledState: 'Disabled'
                  action: 'Block'
                  exclusions: []
                }
              ]
              exclusions: []
            }
          ]
          exclusions: []
        }
        {
          ruleSetType: 'BotProtection'
          ruleSetVersion: 'preview-0.1'
          ruleGroupOverrides: []
          exclusions: []
        }
      ]
    }
  }
}
