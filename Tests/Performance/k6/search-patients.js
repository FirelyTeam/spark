import http from 'k6/http';
import { check } from 'k6';
import { patientValues } from './patients.js';

const COUNT = parseInt(__ENV.COUNT || '100000');
const QUERY = __ENV.QUERY;
const params = { headers: { 'Accept': 'application/fhir+json' }, tags: { query: QUERY } };

export const options = {
  scenarios: { search: { executor: 'constant-vus', vus: parseInt(__ENV.VUS || '10'), duration: __ENV.DURATION || '30s' } },
};

function pick() { return Math.floor(Math.random() * COUNT); }

const queries = {
  // Exactly one match.
  identifier: () => `Patient?identifier=urn:oid:2.16.578.1.12.4.1.4.1|${patientValues(pick()).identifier}`,
  // A handful of matches: about 100000 / 36500 per day.
  birthdate: () => `Patient?birthdate=${patientValues(pick()).birthDate}`,
  // About 2% of the patients share a family name, so this returns around two thousand matches.
  family: () => `Patient?family=${encodeURIComponent(patientValues(pick()).family.slice(0, 4))}&_count=10`,
  // A given name born in a given year: tens of matches.
  given_birthyear: () => { const v = patientValues(pick()); return `Patient?given=${encodeURIComponent(v.given)}&birthdate=${v.birthDate.slice(0, 4)}&_count=10`; },
  // Counting half of the patients.
  gender_count: () => `Patient?gender=female&_summary=count`,
  // Clinical searches, when the data was loaded with CLINICAL=true.
  // Every patient has one blood pressure panel.
  observation_code: () => `Observation?code=http://loinc.org|85354-9&_count=10`,
  // Body weight above a value: a quantity comparison over all patients.
  observation_value: () => `Observation?code=http://loinc.org|29463-7&value-quantity=gt${90 + Math.floor(Math.random() * 10)}|http://unitsofmeasure.org|kg&_count=10`,
  // The observations of one patient, found by the identifier of the patient: a chained search that ends in a token.
  observation_patient: () => `Observation?subject:Patient.identifier=urn:oid:2.16.578.1.12.4.1.4.1|${patientValues(pick()).identifier}&_count=10`,
  // Observations of patients with a family name: a chained search.
  observation_chain: () => `Observation?subject:Patient.family=${encodeURIComponent(patientValues(pick()).family.slice(0, 4))}&_count=10`,
  // Encounters in a month: a date range over periods.
  encounter_date: () => `Encounter?date=${2022 + Math.floor(Math.random() * 4)}-${String(1 + Math.floor(Math.random() * 12)).padStart(2, '0')}&_count=10`,
};

export default function () {
  const res = http.get(`${__ENV.BASE_URL}/${queries[QUERY]()}`, params);
  check(res, { 'ok': (r) => r.status === 200 });
}
