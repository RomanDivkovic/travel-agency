import { emptyWizard, type Service } from './model';
// Shared test fixtures only; never used to populate a visitor's form.
export function validWizard(service: Service = 'complete') {
  const data = emptyWizard(service);
  const year = new Date().getFullYear() + 1;
  data.destination = {
    origin: ' Stockholm ',
    place: ' Lisbon ',
    flexible: false,
    stops: ' Porto\n Coimbra ',
  };
  data.dates = {
    mode: 'fixed',
    oneWay: false,
    departure: `${year}-06-01`,
    return: `${year}-06-12`,
    earliest: `${year}-07-01`,
    latest: `${year}-07-20`,
    duration: '7',
  };
  data.travelers = { adults: '2', children: '1', ages: ['7'], rooms: '2' };
  data.budget = { amount: '25000.50', currency: 'SEK', scope: 'total' };
  data.additional = { activities: true, message: ' Near a station. ' };
  data.contact = {
    name: ' Anna Andersson ',
    email: ' anna@example.com ',
    phone: '',
    method: 'email',
  };
  return data;
}
