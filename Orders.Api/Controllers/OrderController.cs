using Microsoft.AspNetCore.Mvc;
using Orders.Api.DTOs;
using Orders.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orders.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;


        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll([FromQuery] int? usuarioId)
        {
            var ordenes = await _orderService.GetAllAsync(usuarioId);
            return Ok(ordenes);
        }

        [HttpGet("{id:int}")] 
        public async Task<ActionResult<OrderResponse>> GetById(int id)
        {
            
            var orden = await _orderService.GetByIdAsync(id);
            return Ok(orden);
        }

        [HttpGet("internal/check-product/{productoId}")]
        [ApiExplorerSettings(IgnoreApi = true)] 
        public async Task<IActionResult> CheckProductHasOrders(int productoId)
        {
            bool tieneOrdenes = await _orderService.HasOrdersAsync(productoId);
            return Ok(tieneOrdenes);
        }




        [HttpPost]
        public async Task<ActionResult<OrderResponse>> Create([FromBody] OrderRequest request)
        {
            
            var nuevaOrden = await _orderService.CreateAsync(request);

           
            return CreatedAtAction(nameof(GetById), new { id = nuevaOrden.Id }, nuevaOrden);
        }


        [HttpPut("{id:int}/status")]
        public async Task<ActionResult<OrderResponse>> UpdateStatus(int id, [FromBody] StatusUpdateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NuevoEstado))
            {
                return BadRequest(new { Error = "El campo nuevoEstado es obligatorio." });
            }

            var ordenActualizada = await _orderService.UpdateStatusAsync(id, request.NuevoEstado);
            return Ok(ordenActualizada);
        }
    }

    public class StatusUpdateRequest
    {
        public string NuevoEstado { get; set; } = string.Empty;
    }
}