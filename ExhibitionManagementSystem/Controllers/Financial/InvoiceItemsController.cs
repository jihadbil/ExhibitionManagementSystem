using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace ExhibitionManagementSystem.Controllers.Financial
{
    [Route("api/financial/invoices/{invoiceId:int}/items")]
    [Authorize(Policy = "AdminOnly")]
    public class InvoiceItemsController : BaseApiController
    {
        private readonly IInvoiceItemService _invoiceItemService;

        public InvoiceItemsController(IInvoiceItemService invoiceItemService)
        {
            _invoiceItemService = invoiceItemService;
        }

        // GET /api/financial/invoices/{invoiceId}/items
        [HttpGet]
        public async Task<ActionResult<IList<InvoiceItemDto>>> GetItems(int invoiceId)
        {
            var result = await _invoiceItemService.GetItemsByInvoiceIdAsync(TenantId, invoiceId);
            return ToActionResult(result);
        }

        // GET /api/financial/invoices/{invoiceId}/items/{itemId}
        [HttpGet("{itemId:int}")]
        public async Task<ActionResult<InvoiceItemDto>> GetItemById(int invoiceId, int itemId)
        {
            var checkResult = await _invoiceItemService.GetItemByIdAsync(TenantId, itemId);
            if (!checkResult.IsSuccess) return ToActionResult(checkResult);
            if (checkResult.Data!.InvoiceID != invoiceId)
            {
                return BadRequest("البند المحدد لا ينتمي إلى هذه الفاتورة.");
            }
            return Ok(checkResult.Data);
        }

        // POST /api/financial/invoices/{invoiceId}/items
        [HttpPost]
        public async Task<ActionResult<InvoiceItemDto>> CreateItem(int invoiceId, [FromBody] InvoiceItemCreateDto dto)
        {
            var result = await _invoiceItemService.CreateItemAsync(TenantId, invoiceId, dto);
            if (!result.IsSuccess) return ToActionResult(result);
            return CreatedAtAction(nameof(GetItemById), new { invoiceId, itemId = result.Data!.InvoiceItemID }, result.Data);
        }

        // PUT /api/financial/invoices/{invoiceId}/items/{itemId}
        [HttpPut("{itemId:int}")]
        public async Task<ActionResult<InvoiceItemDto>> UpdateItem(int invoiceId, int itemId, [FromBody] InvoiceItemUpdateDto dto)
        {
            if (dto.InvoiceItemID != itemId)
            {
                return BadRequest("معرف البند غير متطابق.");
            }

            var checkResult = await _invoiceItemService.GetItemByIdAsync(TenantId, itemId);
            if (!checkResult.IsSuccess) return ToActionResult(checkResult);
            if (checkResult.Data!.InvoiceID != invoiceId)
            {
                return BadRequest("البند المحدد لا ينتمي إلى هذه الفاتورة.");
            }

            var result = await _invoiceItemService.UpdateItemAsync(TenantId, itemId, dto);
            return ToActionResult(result);
        }

        // DELETE /api/financial/invoices/{invoiceId}/items/{itemId}
        [HttpDelete("{itemId:int}")]
        public async Task<IActionResult> DeleteItem(int invoiceId, int itemId)
        {
            var checkResult = await _invoiceItemService.GetItemByIdAsync(TenantId, itemId);
            if (!checkResult.IsSuccess) return ToActionResult((ServiceResult)checkResult);
            if (checkResult.Data!.InvoiceID != invoiceId)
            {
                return BadRequest("البند المحدد لا ينتمي إلى هذه الفاتورة.");
            }

            var result = await _invoiceItemService.DeleteItemAsync(TenantId, itemId);
            return ToActionResult((ServiceResult)result);
        }
    }
}

