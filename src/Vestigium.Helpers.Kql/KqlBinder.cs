        return field switch
        {
            KqlType.Integer or KqlType.Number when literal is KqlType.Integer or KqlType.Number => true,
            KqlType.DateTime when literal is KqlType.DateTime or KqlType.String => true,
            _ => false
        };