using Mango.Services.OrderAPI.Models.Dto;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace Mango.Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Authorize]
        public IActionResult OrderIndex()
        {
            return View();
        }

        // Action để hiển thị form chọn ngày
        // Action để hiển thị form chọn ngày
        [HttpGet]
        public IActionResult GetDailyIncome()
        {
            return View();
        }

        // Action để xử lý việc gửi yêu cầu lấy doanh thu hàng ngày
        [HttpGet]
        public async Task<IActionResult> GetIncome(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Gọi phương thức từ service để lấy doanh thu hàng ngày
                var response = await _orderService.GetIncome(startDate, endDate);

                if (response != null && response.IsSuccess)
                {
                    // Chuyển đổi dữ liệu trả về thành đối tượng DailyIncomeDto
                    var dailyIncomeDto = JsonConvert.DeserializeObject<DailyIncomeDto>(Convert.ToString(response.Result));

                    // Trả về view "GetDailyIncome" và truyền dữ liệu doanh thu hàng ngày vào view
                    return View("GetDailyIncome", dailyIncomeDto);
                }
                else
                {
                    TempData["error"] = "Failed to retrieve daily income data from API";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = $"An error occurred: {ex.Message}";
            }

            // Nếu có lỗi xảy ra hoặc không thành công, trả về view "GetDailyIncome" với dữ liệu mặc định hoặc thông báo lỗi
            return View("GetDailyIncome", new DailyIncomeDto());
        }





        [Authorize]
        public async Task<IActionResult> OrderDetail(int orderId)
		{
			OrderHeaderDto orderHeaderDto = new OrderHeaderDto();
            string userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;

            var response = await _orderService.GetOrder(orderId);
			if (response != null && response.IsSuccess)
			{
				orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));
			}
			if(!User.IsInRole(SD.RoleAdmin) && !User.IsInRole(SD.RoleChef) && userId!= orderHeaderDto.UserId)
            {
                return NotFound();
            }
            return View(orderHeaderDto);
		}

        [HttpPost("OrderReadyForPickup")]
        public async Task<IActionResult> OrderReadyForPickup(int orderId)
        {
            var response = await _orderService.UpdateOrderStatus(orderId,SD.Status_ReadyForPickup);
            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Status updated successfully";
                return RedirectToAction(nameof(OrderDetail), new { orderId = orderId });
            }
            return View();
        }

        [HttpPost("CompleteOrder")]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var response = await _orderService.UpdateOrderStatus(orderId, SD.Status_Completed);
            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Status updated successfully";
                return RedirectToAction(nameof(OrderDetail), new { orderId = orderId });
            }
            return View();
        }

        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var response = await _orderService.UpdateOrderStatus(orderId, SD.Status_Cancelled);
            if (response != null && response.IsSuccess)
            {
                TempData["success"] = "Status updated successfully";
                return RedirectToAction(nameof(OrderDetail), new { orderId = orderId });
            }
            return View();
        }


        [HttpGet]
        public IActionResult GetAll(string status) 
        {
            IEnumerable<OrderHeaderDto> list;
            string userId = "";
            if (!User.IsInRole(SD.RoleAdmin) || !User.IsInRole(SD.RoleAdmin))
            {
                userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
            }
            ResponseDto response = _orderService.GetAllOrder(userId).GetAwaiter().GetResult();
            if (response != null && response.IsSuccess)
            {
                list = JsonConvert.DeserializeObject<List<OrderHeaderDto>>(Convert.ToString(response.Result));
                switch (status)
                {
                    case "approved":
                        list = list.Where(u => u.Status == SD.Status_Approved);
                        break;
					case "readyforpickup":
						list = list.Where(u => u.Status == SD.Status_ReadyForPickup);
						break;
					case "cancelled":
						list = list.Where(u => u.Status == SD.Status_Cancelled || u.Status == SD.Status_Refunded);
						break;
					default:
						break;
				}
            }
            else
            {
                list = new List<OrderHeaderDto>();
            }
            return Json(new { data = list.OrderByDescending(u=>u.OrderHeaderId) });
        }
    }
}
