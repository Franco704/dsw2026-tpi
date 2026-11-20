using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Tests
{
    public class TestAppointmentSearch
    {
        private readonly IPersistence _persistence = Substitute.For<IPersistence>();
        private readonly AppointmentService _service;

        public TestAppointmentSearch()
        {
            _service = new AppointmentService(
                _persistence,
                Substitute.For<IPatientAccessService>(),
                Substitute.For<ILogger<AppointmentService>>());

            _persistence.Paginate(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<Expression<Func<Appointment, bool>>>(),
                    Arg.Any<Expression<Func<Appointment, DateTime>>>(),
                    Arg.Any<bool>(),
                    Arg.Any<string[]>())
                .Returns(new Pagination<Appointment>(10, 1, 0, []));
        }

        private static AppointmentModel.SearchRequest BuildRequest(
            string? status = null,
            DateTime? date = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? patient = null) =>
            new(null, null, patient, status, date, dateFrom, dateTo);

        [Fact]
        public async Task Search_WhenListHasNoDate_ThenThrowsValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.SearchAppointmentsAsync(BuildRequest(), dateRequired: true));
        }

        [Fact]
        public async Task Search_WhenStatusDoesNotExist_ThenThrowsValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.SearchAppointmentsAsync(BuildRequest(status: "PENDING", date: DateTime.Today)));
        }

        [Fact]
        public async Task Search_WhenRangeIsInverted_ThenThrowsValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.SearchAppointmentsAsync(
                    BuildRequest(dateFrom: DateTime.Today, dateTo: DateTime.Today.AddDays(-1))));
        }

        [Fact]
        public async Task Search_WhenFiltersAreValid_ThenSortsDescending()
        {
            await _service.SearchAppointmentsAsync(
                BuildRequest(status: "booked", dateFrom: DateTime.Today, dateTo: DateTime.Today.AddDays(6), patient: "Pérez"),
                dateRequired: true);

            await _persistence.Received(1).Paginate(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Expression<Func<Appointment, bool>>>(),
                Arg.Any<Expression<Func<Appointment, DateTime>>>(),
                Arg.Is(true),
                Arg.Any<string[]>());
        }
    }
}
