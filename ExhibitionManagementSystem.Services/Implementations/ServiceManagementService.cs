using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Service;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class ServiceManagementService : IServiceManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ServiceManagementService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IList<ServiceDto>>> GetByTenantAsync(int tenantId)
        {
            var services = await _unitOfWork.Services.GetByTenantAsync(tenantId);
            var dtos = _mapper.Map<IList<ServiceDto>>(services);
            return ServiceResult<IList<ServiceDto>>.Success(dtos);
        }

        public async Task<ServiceResult<ServiceDto>> GetByIdAsync(int tenantId, int serviceId)
        {
            var service = await _unitOfWork.Services.GetByIdAsync(serviceId);
            if (service == null || service.TenantID != tenantId)
            {
                return ServiceResult<ServiceDto>.Failure("الخدمة غير موجودة", "SERVICE_NOT_FOUND");
            }

            var dto = _mapper.Map<ServiceDto>(service);
            return ServiceResult<ServiceDto>.Success(dto);
        }

        public async Task<ServiceResult<ServiceDto>> CreateAsync(int tenantId, ServiceCreateDto dto)
        {
            var service = _mapper.Map<Models.Service>(dto);
            service.TenantID = tenantId;
            service.IsActive = true;

            await _unitOfWork.Services.AddAsync(service);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<ServiceDto>(service);
            return ServiceResult<ServiceDto>.Success(resultDto);
        }

        public async Task<ServiceResult<ServiceDto>> UpdateAsync(int tenantId, int serviceId, ServiceCreateDto dto)
        {
            var service = await _unitOfWork.Services.GetByIdAsync(serviceId);
            if (service == null || service.TenantID != tenantId)
            {
                return ServiceResult<ServiceDto>.Failure("الخدمة غير موجودة", "SERVICE_NOT_FOUND");
            }

            _mapper.Map(dto, service);
            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Services.Update(service);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<ServiceDto>(service);
            return ServiceResult<ServiceDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeactivateAsync(int tenantId, int serviceId)
        {
            var service = await _unitOfWork.Services.GetByIdAsync(serviceId);
            if (service == null || service.TenantID != tenantId)
            {
                return ServiceResult.Failure("الخدمة غير موجودة", "SERVICE_NOT_FOUND");
            }

            service.IsActive = false;
            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Services.Update(service);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
    }
}
