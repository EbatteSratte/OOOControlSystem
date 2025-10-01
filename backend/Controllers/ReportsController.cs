using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace OOOControlSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : Controller
    {
        private readonly ApplicationContext _context;
        public ReportsController(ApplicationContext context) { _context = context; }

        [Authorize(Roles = "Customer")]
        [HttpGet("customer-excel")]
        public async Task<IActionResult> CustomerExcel()
        {
            var userIdStr = User.FindFirst("userId")?.Value
                            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized("No userId in token");

            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.OwnerId == userId)
                .OrderBy(p => p.Id)
                .ToListAsync();

            var pids = projects.Select(p => p.Id).ToList();

            var defects = await _context.Defects
                .AsNoTracking()
                .Include(d => d.Project)
                .Include(d => d.AssignedUser)
                .Include(d => d.Reporter)
                .Where(d => pids.Contains(d.ProjectId))
                .OrderBy(d => d.ProjectId).ThenBy(d => d.Id)
                .ToListAsync();

            using var ms = new MemoryStream();
            using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
            {
                var wb = doc.AddWorkbookPart();
                wb.Workbook = new Workbook();

                var styles = BuildStyles(wb);

                var wsP = AddSheet(wb, "Projects");
                var wsProj = wsP.Worksheet;
                wsProj.InsertAt(new Columns(
                    new Column { Min = 1, Max = 1, Width = 8, CustomWidth = true },  // ID
                    new Column { Min = 2, Max = 2, Width = 32, CustomWidth = true },  // Name
                    new Column { Min = 3, Max = 3, Width = 14, CustomWidth = true },  // Status
                    new Column { Min = 4, Max = 4, Width = 21, CustomWidth = true },  // CreatedAt
                    new Column { Min = 5, Max = 5, Width = 60, CustomWidth = true }   // Description
                ), 0);
                ApplyFreezeTopRow(wsProj);
                var sdP = wsProj.GetFirstChild<SheetData>()!;
                AddHeader(sdP, styles.Header, "ID", "Name", "Status", "CreatedAt", "Description");

                foreach (var p in projects)
                {
                    AddRow(sdP,
                        Text(p.Id, styles.TextBorder),
                        Text(p.Name, styles.TextBorder),
                        Text(p.Status.ToString(), styles.TextCenterBorder),
                        Text(DateTimeToStr(p.CreatedAt), styles.DateBorder),
                        Text(p.Description, styles.WrapBorder));
                }
                SetAutoFilter(wsProj, "A1:E" + (projects.Count + 1));

                var wsD = AddSheet(wb, "Defects");
                var wsDef = wsD.Worksheet;

                wsDef.InsertAt(new Columns(
                    new Column { Min = 1, Max = 1, Width = 8, CustomWidth = true },  // ID
                    new Column { Min = 2, Max = 2, Width = 40, CustomWidth = true },  // Title
                    new Column { Min = 3, Max = 3, Width = 14, CustomWidth = true },  // Status
                    new Column { Min = 4, Max = 4, Width = 12, CustomWidth = true },  // Priority
                    new Column { Min = 5, Max = 5, Width = 28, CustomWidth = true },  // ProjectName
                    new Column { Min = 6, Max = 6, Width = 28, CustomWidth = true },  // AssignedToName
                    new Column { Min = 7, Max = 7, Width = 28, CustomWidth = true },  // ReporterName
                    new Column { Min = 8, Max = 8, Width = 21, CustomWidth = true },  // CreatedAt
                    new Column { Min = 9, Max = 9, Width = 21, CustomWidth = true },  // UpdatedAt
                    new Column { Min = 10, Max = 10, Width = 60, CustomWidth = true }   // Description
                ), 0);

                ApplyFreezeTopRow(wsDef);
                var sdD = wsDef.GetFirstChild<SheetData>()!;
                AddHeader(sdD, styles.Header,
                    "ID", "Title", "Status", "Priority",
                    "ProjectName", "AssignedToName", "ReporterName",
                    "CreatedAt", "UpdatedAt", "Description");

                foreach (var d in defects)
                {
                    AddRow(sdD,
                        Text(d.Id, styles.TextBorder),
                        Text(d.Title, styles.WrapBorder),
                        Text(d.Status.ToString(), styles.TextCenterBorder),
                        Text(d.Priority.ToString(), styles.TextCenterBorder),
                        Text(d.Project?.Name, styles.TextBorder),
                        Text(d.AssignedUser?.FullName, styles.TextBorder),
                        Text(d.Reporter?.FullName, styles.TextBorder),
                        Text(DateTimeToStr(d.CreatedAt), styles.DateBorder),
                        Text(DateTimeToStr(d.UpdatedAt), styles.DateBorder),
                        Text(d.Description, styles.WrapBorder));
                }

                SetAutoFilter(wsDef, "A1:J" + (defects.Count + 1));

                var sheets = wb.Workbook.AppendChild(new Sheets());
                uint sid = 1;
                sheets.Append(new Sheet { Id = wb.GetIdOfPart(wsP), SheetId = sid++, Name = "Projects" });
                sheets.Append(new Sheet { Id = wb.GetIdOfPart(wsD), SheetId = sid++, Name = "Defects" });
                wb.Workbook.Save();
            }

            var fileName = $"customer_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        private static string DateTimeToStr(DateTime? dt) =>
            dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty;

        private static void ApplyFreezeTopRow(Worksheet ws)
        {
            var views = new SheetViews();
            var view = new SheetView { WorkbookViewId = 0U };
            view.Append(new Pane
            {
                VerticalSplit = 1D,
                TopLeftCell = "A2",
                ActivePane = PaneValues.BottomLeft,
                State = PaneStateValues.Frozen
            });
            views.Append(view);
            ws.InsertAt(views, 0);
        }

        private static void SetAutoFilter(Worksheet ws, string range)
        {
            var af = new AutoFilter { Reference = range };
            ws.Append(af);
        }

        private static WorksheetPart AddSheet(WorkbookPart wb, string name)
        {
            var ws = wb.AddNewPart<WorksheetPart>();
            ws.Worksheet = new Worksheet(new SheetData());
            return ws;
        }

        private static void AddHeader(SheetData sd, uint headerStyleIndex, params string[] heads)
        {
            var r = new Row();
            foreach (var h in heads)
                r.Append(new Cell { StyleIndex = headerStyleIndex, DataType = CellValues.String, CellValue = new CellValue(h) });
            sd.Append(r);
        }

        private static void AddRow(SheetData sd, params Cell[] cells)
        {
            var r = new Row();
            foreach (var c in cells) r.Append(c);
            sd.Append(r);
        }

        private static Cell Text(object? v, uint styleIndex) =>
            new Cell { StyleIndex = styleIndex, DataType = CellValues.String, CellValue = new CellValue(v?.ToString() ?? string.Empty) };

        private class StyleRefs
        {
            public uint Header;
            public uint TextBorder;
            public uint TextCenterBorder;
            public uint WrapBorder;
            public uint DateBorder;
        }

        private static StyleRefs BuildStyles(WorkbookPart wb)
        {
            var sp = wb.AddNewPart<WorkbookStylesPart>();
            var stylesheet = new Stylesheet();

            var nfs = new NumberingFormats() { Count = 0U };

            var fonts = new Fonts() { Count = 3U };
            fonts.Append(new Font(
                new FontSize() { Val = 11D },
                new Color() { Theme = 1U },
                new FontName() { Val = "Calibri" }
            ));
            fonts.Append(new Font(
                new Bold(),
                new Color() { Rgb = HexBinaryValue.FromString("FFFFFFFF") },
                new FontSize() { Val = 11D },
                new FontName() { Val = "Calibri" }
            ));

            var fills = new Fills() { Count = 3U };
            fills.Append(new Fill(new PatternFill { PatternType = PatternValues.None }));
            fills.Append(new Fill(new PatternFill { PatternType = PatternValues.Gray125 }));
            fills.Append(new Fill(new PatternFill
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = new ForegroundColor { Rgb = HexBinaryValue.FromString("FF1F2937") },
                BackgroundColor = new BackgroundColor { Indexed = 64U }
            }));

            var borders = new Borders() { Count = 2U };
            borders.Append(new Border());
            borders.Append(new Border(
                new LeftBorder { Style = BorderStyleValues.Thin },
                new RightBorder { Style = BorderStyleValues.Thin },
                new TopBorder { Style = BorderStyleValues.Thin },
                new BottomBorder { Style = BorderStyleValues.Thin },
                new DiagonalBorder()
            ));

            var cfs = new CellFormats();
            cfs.Append(new CellFormat() { FontId = 0U, FillId = 0U, BorderId = 0U, ApplyFont = true, ApplyFill = true, ApplyBorder = true });

            cfs.Append(new CellFormat
            {
                FontId = 1U,
                FillId = 2U,
                BorderId = 1U,
                ApplyFont = true,
                ApplyFill = true,
                ApplyBorder = true,
                ApplyAlignment = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
            });
            uint headerIdx = 1U;

            cfs.Append(new CellFormat { FontId = 0U, FillId = 0U, BorderId = 1U, ApplyFont = true, ApplyBorder = true });
            uint textIdx = 2U;

            cfs.Append(new CellFormat
            {
                FontId = 0U,
                FillId = 0U,
                BorderId = 1U,
                ApplyFont = true,
                ApplyBorder = true,
                ApplyAlignment = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
            });
            uint textCenterIdx = 3U;

            cfs.Append(new CellFormat
            {
                FontId = 0U,
                FillId = 0U,
                BorderId = 1U,
                ApplyFont = true,
                ApplyBorder = true,
                ApplyAlignment = true,
                Alignment = new Alignment { WrapText = true, Vertical = VerticalAlignmentValues.Top }
            });
            uint wrapIdx = 4U;

            cfs.Append(new CellFormat { FontId = 0U, FillId = 0U, BorderId = 1U, ApplyFont = true, ApplyBorder = true });
            uint dateIdx = 5U;

            cfs.Count = (uint)cfs.ChildElements.Count;

            stylesheet.Append(nfs);
            stylesheet.Append(fonts);
            stylesheet.Append(fills);
            stylesheet.Append(borders);
            stylesheet.Append(cfs);
            sp.Stylesheet = stylesheet;
            sp.Stylesheet.Save();

            return new StyleRefs
            {
                Header = headerIdx,
                TextBorder = textIdx,
                TextCenterBorder = textCenterIdx,
                WrapBorder = wrapIdx,
                DateBorder = dateIdx
            };
        }
    }
}
