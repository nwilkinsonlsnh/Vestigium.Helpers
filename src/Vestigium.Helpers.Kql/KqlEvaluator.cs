        if (left.Type is KqlType.Integer or KqlType.Number || right.Type is KqlType.Integer or KqlType.Number)
        {
            if (!TryNumber(left.Raw, out var lv) || !TryNumber(right.Value, out var rv))
                return null;
            return lv.CompareTo(rv);
        }

        if (left.Type == KqlType.DateTime || right.Type == KqlType.DateTime)
        {
            if (!TryDate(left.Raw, out var ld) || !TryDate(right.Value, out var rd))
                return null;
            return ld.CompareTo(rd);
        }