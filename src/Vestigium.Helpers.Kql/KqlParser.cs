            case { Kind: KqlTokenKind.MacAddress }:
                Advance();
                return new KqlLiteral { Type = KqlType.String, Value = token.Text, Form = KqlLiteralForm.MacAddress };
            case { Kind: KqlTokenKind.DateValue }:
                Advance();
                if (!KqlDates.TryParse(token.Text, out var date))
                    throw Error(token, "invalid date");
                return new KqlLiteral { Type = KqlType.DateTime, Value = date };
            case { Kind: KqlTokenKind.Ident }: