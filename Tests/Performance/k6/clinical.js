// Clinical resources for a patient: an encounter with the practitioner and organization that handled it, the
// observations measured during it, the conditions diagnosed and the medication prescribed. Everything is derived
// from the patient number, so every store gets identical data and the searches know what exists.

const LOINC = 'http://loinc.org';
const SNOMED = 'http://snomed.info/sct';
const UCUM = 'http://unitsofmeasure.org';
const ACT_CODE = 'http://terminology.hl7.org/CodeSystem/v3-ActCode';
const OBSERVATION_CATEGORY = 'http://terminology.hl7.org/CodeSystem/observation-category';

export const PRACTITIONERS = 100;
export const ORGANIZATIONS = 20;

function mix(i, salt) {
  let x = (i * 2654435761 + salt * 40503) >>> 0;
  x ^= x >>> 16; x = Math.imul(x, 2246822507) >>> 0; x ^= x >>> 13; x = Math.imul(x, 3266489909) >>> 0; x ^= x >>> 16;
  return x >>> 0;
}

/** A value between min and max, derived from the patient number. */
function between(i, salt, min, max) {
  return min + (mix(i, salt) % (max - min + 1));
}

// The practitioners and organizations every patient is spread over, so that references and chained searches have
// something to fan out to.
const PRACTITIONER_FAMILIES = ['Legen', 'Doktorsen', 'Fastlegen', 'Kirurgsen', 'Indremedisin', 'Barnlegen'];
const SPECIALTIES = [
  { code: '419772000', display: 'Family practice' },
  { code: '394814009', display: 'General practice' },
  { code: '408443003', display: 'General medical practice' },
];
const ORGANIZATION_NAMES = ['Universitetssykehuset', 'Legevakten', 'Helsehuset', 'Distriktsmedisinsk senter', 'Fastlegekontoret'];

export function practitioner(n) {
  return {
    resourceType: 'Practitioner',
    active: true,
    identifier: [{ system: 'urn:oid:2.16.578.1.12.4.1.4.4', value: String(900000 + n) }],
    name: [{ family: `${PRACTITIONER_FAMILIES[n % PRACTITIONER_FAMILIES.length]}-${n}`, given: ['Doktor'], prefix: ['Dr.'] }],
    gender: n % 2 === 0 ? 'female' : 'male',
  };
}

export function organization(n) {
  return {
    resourceType: 'Organization',
    active: true,
    identifier: [{ system: 'urn:oid:2.16.578.1.12.4.1.2', value: String(800000 + n) }],
    name: `${ORGANIZATION_NAMES[n % ORGANIZATION_NAMES.length]} ${n}`,
    type: [{ coding: [{ system: 'http://terminology.hl7.org/CodeSystem/organization-type', code: 'prov', display: 'Healthcare Provider' }] }],
    address: [{ city: 'Tromsø', postalCode: String(9000 + (n % 100)), country: 'NO' }],
  };
}

const ENCOUNTER_CLASSES = [
  { code: 'AMB', display: 'ambulatory' },
  { code: 'EMER', display: 'emergency' },
  { code: 'IMP', display: 'inpatient encounter' },
];

export function encounterValues(i) {
  // Encounters spread over the last four years, most of them short.
  const start = new Date(Date.UTC(2022, 0, 1) + (mix(i, 11) % 1460) * 86400000 + (mix(i, 12) % 24) * 3600000);
  const hours = mix(i, 17) % 10 === 0 ? 72 : 1;
  return {
    start: start.toISOString(),
    end: new Date(start.getTime() + hours * 3600000).toISOString(),
    day: start.toISOString().slice(0, 10),
    class: ENCOUNTER_CLASSES[mix(i, 18) % ENCOUNTER_CLASSES.length],
    practitioner: mix(i, 19) % PRACTITIONERS,
    organization: mix(i, 20) % ORGANIZATIONS,
  };
}

export function encounter(i, patient, practitionerReference, organizationReference) {
  const v = encounterValues(i);
  return {
    resourceType: 'Encounter',
    status: 'finished',
    class: { system: ACT_CODE, code: v.class.code, display: v.class.display },
    type: [{ coding: [{ system: SNOMED, code: '162673000', display: 'General examination of patient' }] }],
    subject: { reference: patient },
    participant: [{ individual: { reference: practitionerReference } }],
    serviceProvider: { reference: organizationReference },
    period: { start: v.start, end: v.end },
  };
}

function observation(patient, visit, performer, effective, category, code, display, extra) {
  return {
    resourceType: 'Observation',
    status: 'final',
    category: [{ coding: [{ system: OBSERVATION_CATEGORY, code: category }] }],
    code: { coding: [{ system: LOINC, code, display }] },
    subject: { reference: patient },
    encounter: { reference: visit },
    performer: [{ reference: performer }],
    effectiveDateTime: effective,
    ...extra,
  };
}

function quantity(value, unit, code) {
  return { valueQuantity: { value, unit, system: UCUM, code } };
}

/**
 * Vital signs measured at the encounter and the laboratory results that came back from it: a blood pressure panel
 * with components, heart rate, weight, height, temperature, and glucose, haemoglobin, cholesterol and creatinine.
 * Smoking status is a coded value rather than a quantity.
 */
export function observations(i, patient, visit, performer) {
  const v = encounterValues(i);
  const laboratory = new Date(new Date(v.start).getTime() + 7200000).toISOString();
  const vital = (code, display, value) => observation(patient, visit, performer, v.start, 'vital-signs', code, display, value);
  const lab = (code, display, value) => observation(patient, visit, performer, laboratory, 'laboratory', code, display, value);

  return [
    vital('85354-9', 'Blood pressure panel', {
      component: [
        {
          code: { coding: [{ system: LOINC, code: '8480-6', display: 'Systolic blood pressure' }] },
          ...quantity(between(i, 13, 100, 180), 'mmHg', 'mm[Hg]'),
        },
        {
          code: { coding: [{ system: LOINC, code: '8462-4', display: 'Diastolic blood pressure' }] },
          ...quantity(between(i, 14, 55, 110), 'mmHg', 'mm[Hg]'),
        },
      ],
    }),
    vital('8867-4', 'Heart rate', quantity(between(i, 15, 45, 120), '/min', '/min')),
    vital('29463-7', 'Body weight', quantity(between(i, 16, 45, 130), 'kg', 'kg')),
    vital('8302-2', 'Body height', quantity(between(i, 21, 150, 200), 'cm', 'cm')),
    vital('8310-5', 'Body temperature', quantity(360 + (mix(i, 22) % 30) / 10, 'Cel', 'Cel')),
    lab('2339-0', 'Glucose', quantity(between(i, 23, 35, 150) / 10, 'mmol/L', 'mmol/L')),
    lab('718-7', 'Haemoglobin', quantity(between(i, 24, 90, 180), 'g/L', 'g/L')),
    lab('2093-3', 'Cholesterol', quantity(between(i, 25, 30, 90) / 10, 'mmol/L', 'mmol/L')),
    lab('2160-0', 'Creatinine', quantity(between(i, 26, 40, 150), 'umol/L', 'umol/L')),
    // A coded value instead of a quantity.
    observation(patient, visit, performer, v.start, 'social-history', '72166-2', 'Tobacco smoking status', {
      valueCodeableConcept: {
        coding: [mix(i, 27) % 3 === 0
          ? { system: LOINC, code: 'LA18978-9', display: 'Never smoker' }
          : { system: LOINC, code: 'LA18976-3', display: 'Current every day smoker' }],
      },
    }),
  ];
}

const CONDITIONS = [
  { code: '38341003', display: 'Hypertension' },
  { code: '44054006', display: 'Type 2 diabetes mellitus' },
  { code: '195967001', display: 'Asthma' },
  { code: '13645005', display: 'Chronic obstructive lung disease' },
  { code: '35489007', display: 'Depressive disorder' },
  { code: '396275006', display: 'Osteoarthritis' },
];

/** One or two conditions, diagnosed some years before the encounter. */
export function conditions(i, patient, visit, asserter) {
  const v = encounterValues(i);
  const count = mix(i, 28) % 3 === 0 ? 2 : 1;
  return Array.from({ length: count }, (_, n) => {
    const condition = CONDITIONS[mix(i, 29 + n) % CONDITIONS.length];
    const onset = new Date(new Date(v.start).getTime() - between(i, 31 + n, 30, 3650) * 86400000);
    return {
      resourceType: 'Condition',
      clinicalStatus: { coding: [{ system: 'http://terminology.hl7.org/CodeSystem/condition-clinical', code: 'active' }] },
      verificationStatus: { coding: [{ system: 'http://terminology.hl7.org/CodeSystem/condition-ver-status', code: 'confirmed' }] },
      category: [{ coding: [{ system: 'http://terminology.hl7.org/CodeSystem/condition-category', code: 'encounter-diagnosis' }] }],
      code: { coding: [{ system: SNOMED, code: condition.code, display: condition.display }] },
      subject: { reference: patient },
      encounter: { reference: visit },
      asserter: { reference: asserter },
      onsetDateTime: onset.toISOString().slice(0, 10),
    };
  });
}

const MEDICATIONS = [
  { code: '386864001', display: 'Metformin' },
  { code: '372756006', display: 'Simvastatin' },
  { code: '387458008', display: 'Aspirin' },
  { code: '318272007', display: 'Amlodipine' },
  { code: '395726003', display: 'Salbutamol' },
];

/** A prescription from the encounter. */
export function medicationRequest(i, patient, visit, requester) {
  const v = encounterValues(i);
  const medication = MEDICATIONS[mix(i, 33) % MEDICATIONS.length];
  return {
    resourceType: 'MedicationRequest',
    status: 'active',
    intent: 'order',
    medicationCodeableConcept: { coding: [{ system: SNOMED, code: medication.code, display: medication.display }] },
    subject: { reference: patient },
    encounter: { reference: visit },
    requester: { reference: requester },
    authoredOn: v.start,
    dosageInstruction: [{ text: 'One tablet daily', timing: { repeat: { frequency: 1, period: 1, periodUnit: 'd' } } }],
  };
}

export { SPECIALTIES };
