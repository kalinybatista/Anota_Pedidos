using Anota_Pedidos.Data;
using Anota_Pedidos.Filters;
using Anota_Pedidos.Models;
using Anota_Pedidos.Services;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Anota_Pedidos.Controllers
{
    [RequireHttps]
    [ServiceFilter(typeof(AuthFilter))]

    [Route("api/vendas")]
    [ApiController]
    public class VendasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICryptoService _cryptoService;  // 🔥 ADICIONAR

        public VendasController(ApplicationDbContext context, ICryptoService cryptoService)  // 🔥 ADICIONAR PARÂMETRO
        {
            _context = context;
            _cryptoService = cryptoService;  // 🔥 ADICIONAR
        }

        // GET: api/vendas/data
        [HttpGet("data")]
        public async Task<IActionResult> GetVendasData(string period = "today", string search = "", int page = 1, int pageSize = 10)
        {
            var query = _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Itens)
                .Where(p => p.Status_Pedido == StatusPedido.FINALIZADO);

            // --- Filtro por período ---
            var now = DateTime.Now;
            DateTime startDate, endDate;
            switch (period)
            {
                case "today":
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddSeconds(-1);
                    break;
                case "week":
                    startDate = now.AddDays(-(int)now.DayOfWeek).Date;
                    endDate = startDate.AddDays(7).AddSeconds(-1);
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1).AddSeconds(-1);
                    break;
                case "year":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = new DateTime(now.Year, 12, 31, 23, 59, 59);
                    break;
                default:
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddSeconds(-1);
                    break;
            }
            query = query.Where(p => p.Data_Pedido >= startDate && p.Data_Pedido <= endDate);

            // --- Busca por nome, código ou telefone ---
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Usuario.Nome.Contains(search) ||
                    p.Id_Pedido.ToString().Contains(search) ||
                    p.Usuario.Telefone.Contains(search));
            }

            // --- Total de itens e páginas ---
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // --- Buscar dados antes de descriptografar ---
            var vendasRaw = await query
                .OrderByDescending(p => p.Data_Pedido)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id_Pedido,
                    cliente = p.Usuario.Nome,
                    telefoneCriptografado = p.Usuario.Telefone,
                    codigo = p.CodigoPedido,
                    data = p.Data_Pedido,
                    itens = p.Itens.Count,
                    pagamento = p.FormaPagamento,
                    valor = p.Valor_Total ?? 0
                })
                .ToListAsync();

            // 🔥 DESCRIPTOGRAFAR OS TELEFONES
            var vendas = vendasRaw.Select(v => new
            {
                v.id,
                v.cliente,
                telefone = DescriptografarTelefone(v.telefoneCriptografado),
                v.codigo,
                data = v.data.ToString("dd/MM/yyyy HH:mm"),
                v.itens,
                v.pagamento,
                status = "Pago",
                v.valor
            }).ToList();

            var totalVendas = totalItems;
            var totalFaturamento = await query.SumAsync(p => p.Valor_Total) ?? 0;

            return Ok(new
            {
                success = true,
                data = vendas,
                totalItems,
                totalPages,
                currentPage = page,
                pageSize,
                totalVendas,
                totalFaturamento
            });
        }

        // 🔥 MÉTODO AUXILIAR PARA DESCRIPTOGRAFAR TELEFONE
        private string DescriptografarTelefone(string telefoneCriptografado)
        {
            if (string.IsNullOrEmpty(telefoneCriptografado))
                return "-";

            try
            {
                var telefoneDescriptografado = _cryptoService.Decrypt(telefoneCriptografado);

                // Formatar telefone (XX) XXXXX-XXXX
                if (telefoneDescriptografado.Length == 11)
                {
                    return Convert.ToUInt64(telefoneDescriptografado).ToString(@"\(00\) 00000\-0000");
                }
                else if (telefoneDescriptografado.Length == 10)
                {
                    return Convert.ToUInt64(telefoneDescriptografado).ToString(@"\(00\) 0000\-0000");
                }
                return telefoneDescriptografado;
            }
            catch
            {
                return telefoneCriptografado; // Se não conseguir descriptografar, mostra como está
            }
        }

        // GET: api/vendas/detalhes/{id}
        [HttpGet("detalhes/{id}")]
        public async Task<IActionResult> ObterDetalhes(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id_Pedido == id && p.Status_Pedido == StatusPedido.FINALIZADO);

            if (pedido == null)
                return NotFound(new { success = false, message = "Pedido não encontrado" });

            // 🔥 DESCRIPTOGRAFAR O TELEFONE
            string telefoneDescriptografado = DescriptografarTelefone(pedido.Usuario?.Telefone);

            return Ok(new
            {
                success = true,
                id = pedido.Id_Pedido,
                cliente = pedido.Usuario?.Nome ?? "Cliente",
                codigo = pedido.CodigoPedido,
                data = pedido.Data_Pedido.ToString("dd/MM/yyyy HH:mm"),
                telefone = telefoneDescriptografado,
                pagamento = pedido.FormaPagamento,
                valor = pedido.Valor_Total ?? 0,
                itens = pedido.Itens.Select(i => new
                {
                    nomeProduto = i.Produto?.Nome_Produto ?? "Produto",
                    quantidade = i.Quantidade,
                    precoUnitario = i.Valor_Unitario
                })
            });
        }

        // GET: api/vendas/exportar-pdf
        [HttpGet("exportar-pdf")]
        public async Task<IActionResult> ExportarPDF(string period = "today", string search = "")
        {
            var query = _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Itens)
                .Where(p => p.Status_Pedido == StatusPedido.FINALIZADO);

            // Filtro por período
            var now = DateTime.Now;
            DateTime startDate, endDate;
            string periodoTexto = "";

            switch (period)
            {
                case "today":
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddSeconds(-1);
                    periodoTexto = "Hoje";
                    break;
                case "week":
                    startDate = now.AddDays(-(int)now.DayOfWeek).Date;
                    endDate = startDate.AddDays(7).AddSeconds(-1);
                    periodoTexto = "Esta semana";
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1).AddSeconds(-1);
                    periodoTexto = "Este mês";
                    break;
                case "year":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = new DateTime(now.Year, 12, 31, 23, 59, 59);
                    periodoTexto = "Este ano";
                    break;
                default:
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddSeconds(-1);
                    periodoTexto = "Hoje";
                    break;
            }

            query = query.Where(p => p.Data_Pedido >= startDate && p.Data_Pedido <= endDate);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Usuario.Nome.Contains(search) ||
                    p.Id_Pedido.ToString().Contains(search) ||
                    p.Usuario.Telefone.Contains(search));
            }

            var vendasRaw = await query
                .OrderByDescending(p => p.Data_Pedido)
                .Select(p => new
                {
                    Cliente = p.Usuario.Nome,
                    TelefoneCriptografado = p.Usuario.Telefone,
                    Codigo = p.CodigoPedido,
                    Data = p.Data_Pedido,
                    Pagamento = p.FormaPagamento,
                    Valor = p.Valor_Total ?? 0
                })
                .ToListAsync();

            // 🔥 DESCRIPTOGRAFAR OS TELEFONES PARA O PDF
            var vendas = vendasRaw.Select(v => new
            {
                v.Cliente,
                Telefone = DescriptografarTelefone(v.TelefoneCriptografado),
                v.Codigo,
                v.Data,
                v.Pagamento,
                v.Valor
            }).ToList();

            // Calcular totais
            var totalVendas = vendas.Count;
            var valorTotal = vendas.Sum(v => v.Valor);
            var valorMedio = totalVendas > 0 ? valorTotal / totalVendas : 0;

            // Agrupar por forma de pagamento
            var pagamentosPorTipo = vendas
                .GroupBy(v => v.Pagamento ?? "Dinheiro")
                .Select(g => new { Tipo = g.Key, Quantidade = g.Count(), Total = g.Sum(v => v.Valor) })
                .ToList();

            // --- Gerar PDF Personalizado ---
            using var ms = new MemoryStream();

            // Configurar página
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4);
            document.SetMargins(40, 40, 40, 40);

            // ==================== CABEÇALHO ====================
            // Título principal
            var title = new Paragraph("📊 RELATÓRIO DE VENDAS")
                .SetFontSize(24)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(15, 23, 42), 10)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE)
                .SetPadding(15);
            document.Add(title);

            document.Add(new Paragraph("\n"));

            // Informações do estabelecimento
            var estabelecimento = await _context.Estabelecimentos.FirstOrDefaultAsync();
            var estabelecimentoNome = estabelecimento?.Nome_Estabelecimento ?? "Anota Pedidos";

            var headerInfo = new Paragraph($"{estabelecimentoNome}")
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.DARK_GRAY);
            document.Add(headerInfo);

            document.Add(new Paragraph($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY));

            document.Add(new Paragraph("\n"));

            // ==================== RESUMO DO PERÍODO ====================
            var periodBox = new Paragraph()
                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(240, 240, 240), 5)
                .SetPadding(10);

            periodBox.Add(new Paragraph($"📅 Período: {periodoTexto}")
                .SetFontSize(12));
            periodBox.Add(new Paragraph($"📆 De {startDate:dd/MM/yyyy} até {endDate:dd/MM/yyyy}")
                .SetFontSize(11));

            if (!string.IsNullOrWhiteSpace(search))
            {
                periodBox.Add(new Paragraph($"🔍 Busca: \"{search}\"")
                    .SetFontSize(11));
            }

            document.Add(periodBox);
            document.Add(new Paragraph("\n"));

            // ==================== CARD DE TOTAIS ====================
            float[] totalWidths = { 180f, 180f, 180f };
            var totalsTable = new Table(totalWidths).SetWidth(540).SetMarginBottom(15);

            // Card 1 - Total de Vendas
            var cell1 = new Cell()
                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(65, 132, 234))
                .SetPadding(10);
            cell1.Add(new Paragraph("💰 TOTAL DE VENDAS")
                .SetFontSize(10)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER));
            cell1.Add(new Paragraph($"{totalVendas}")
                .SetFontSize(24)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER));
            totalsTable.AddCell(cell1);

            // Card 2 - Valor Total
            var cell2 = new Cell()
                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(34, 197, 94))
                .SetPadding(10);
            cell2.Add(new Paragraph("💵 VALOR TOTAL")
                .SetFontSize(10)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER));
            cell2.Add(new Paragraph($"R$ {valorTotal:F2}")
                .SetFontSize(24)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER));
            totalsTable.AddCell(cell2);

            // Card 3 - Ticket Médio
            var cell3 = new Cell()
                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(245, 158, 11))
                .SetPadding(10);
            cell3.Add(new Paragraph("🎫 TICKET MÉDIO")
                .SetFontSize(10)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER));
            cell3.Add(new Paragraph($"R$ {valorMedio:F2}")
                .SetFontSize(24)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER));
            totalsTable.AddCell(cell3);

            document.Add(totalsTable);
            document.Add(new Paragraph("\n"));

            // ==================== RESULTADO POR FORMA DE PAGAMENTO ====================
            if (pagamentosPorTipo.Any())
            {
                var paymentTitle = new Paragraph("💳 VENDAS POR FORMA DE PAGAMENTO")
                    .SetFontSize(14)
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.DARK_GRAY);
                document.Add(paymentTitle);
                document.Add(new Paragraph("\n"));

                var paymentTable = new Table(3);
                paymentTable.AddHeaderCell("Forma de Pagamento");
                paymentTable.AddHeaderCell("Quantidade");
                paymentTable.AddHeaderCell("Valor Total");

                foreach (var p in pagamentosPorTipo)
                {
                    paymentTable.AddCell(p.Tipo);
                    paymentTable.AddCell(p.Quantidade.ToString());
                    paymentTable.AddCell($"R$ {p.Total:F2}");
                }

                document.Add(paymentTable);
                document.Add(new Paragraph("\n"));
            }

            // ==================== LISTA DE VENDAS ====================
            var listTitle = new Paragraph("📋 LISTA DE VENDAS")
                .SetFontSize(14)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.DARK_GRAY);
            document.Add(listTitle);
            document.Add(new Paragraph("\n"));

            // Tabela de vendas
            var table = new Table(6);
            table.UseAllAvailableWidth();
            table.SetHorizontalAlignment(HorizontalAlignment.CENTER);

            // Cabeçalho da tabela
            table.AddHeaderCell(new Cell().Add(new Paragraph("Cliente").SetFontSize(10)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Telefone").SetFontSize(10)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Código").SetFontSize(10)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Data/Hora").SetFontSize(10)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Pagamento").SetFontSize(10)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Valor").SetFontSize(10)));

            // Dados da tabela
            foreach (var v in vendas)
            {
                table.AddCell(new Cell().Add(new Paragraph(v.Cliente).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(v.Telefone ?? "-").SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(v.Codigo.ToString()).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(v.Data.ToString("dd/MM/yyyy HH:mm")).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(v.Pagamento ?? "Dinheiro").SetFontSize(9)));

                var valorCell = new Cell().Add(new Paragraph($"R$ {v.Valor:F2}").SetFontSize(9));
                if (v.Valor > 100)
                    valorCell.SetFontColor(new iText.Kernel.Colors.DeviceRgb(34, 197, 94));
                else if (v.Valor > 50)
                    valorCell.SetFontColor(new iText.Kernel.Colors.DeviceRgb(245, 158, 11));

                table.AddCell(valorCell);
            }

            document.Add(table);
            document.Add(new Paragraph("\n"));

            // ==================== RODAPÉ COM TOTAIS ====================
            // Linha separadora
            var separator = new Paragraph("─".PadRight(80, '─'))
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(separator);

            // Resumo final
            var summaryTable = new Table(2).SetWidth(400).SetHorizontalAlignment(HorizontalAlignment.CENTER);
            summaryTable.SetMarginTop(15);

            summaryTable.AddCell(new Cell().Add(new Paragraph("📊 TOTAL DE VENDAS:").SetFontSize(11)));
            summaryTable.AddCell(new Cell().Add(new Paragraph($"{totalVendas}").SetFontSize(11).SetTextAlignment(TextAlignment.RIGHT)));

            summaryTable.AddCell(new Cell().Add(new Paragraph("💰 VALOR TOTAL:").SetFontSize(11)));
            summaryTable.AddCell(new Cell().Add(new Paragraph($"R$ {valorTotal:F2}").SetFontSize(11).SetTextAlignment(TextAlignment.RIGHT).SetFontColor(new iText.Kernel.Colors.DeviceRgb(34, 197, 94))));

            summaryTable.AddCell(new Cell().Add(new Paragraph("🎫 TICKET MÉDIO:").SetFontSize(11)));
            summaryTable.AddCell(new Cell().Add(new Paragraph($"R$ {valorMedio:F2}").SetFontSize(11).SetTextAlignment(TextAlignment.RIGHT).SetFontColor(new iText.Kernel.Colors.DeviceRgb(245, 158, 11))));

            document.Add(summaryTable);
            document.Add(new Paragraph("\n"));

            // Rodapé
            var footer = new Paragraph("📱 Anota Pedidos - Sistema de Gestão")
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY);
            document.Add(footer);

            document.Close();

            // Nome do arquivo com período
            string fileName = $"vendas_{period}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            return File(ms.ToArray(), "application/pdf", fileName);
        }
    }
}