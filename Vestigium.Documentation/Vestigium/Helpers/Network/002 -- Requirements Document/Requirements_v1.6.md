| 84 | Flat open row | `PortScanOpenRow` is host, port, transport, service, MSS, banner, elapsed. |
| 85 | Rows is the door | `PortScanCsv.Rows` returns `IReadOnlyList<PortScanOpenRow>`. Pure. No I/O. |
| 86 | CSV stays | `PortScanCsv.Write` writes the same rows. Banner stays quoted. No file write. |
| 87 | Nested rows stay | `PortScanHostRow` is unchanged. It answers what is open on this box. |
| 88 | Open ports only | Rows come from open TCP and open UDP. Closed and timed-out are not in the list. |
