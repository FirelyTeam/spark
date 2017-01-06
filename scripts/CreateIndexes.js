print("indexes for just about any operation, including create / update");
printjson(db.searchindex.createIndex({ "internal_resource" : 1, "identifier.code" : 1, "identifier.system" : 1}, { "name" : "ix_resource_identifier", "background" : true}));
printjson(db.searchindex.createIndex({"internal_id":1, "internal_selflink":1},{"name":"ix_internal_id_selflink", "background":true}));
printjson(db.resources.createIndex({"@REFERENCE" : 1, "@state" : 1}, { "name" : "ix_REFERENCE_state", "background" : "true" }));

print("indexes for when you query by Patient.name or Patient.family a lot");
printjson(db.searchindex.createIndex({"name" : 1}, { "name" : "ix_Patient_name", partialFilterExpression : { "internal_resource" : "Patient" }, "background" : "true" }));
printjson(db.searchindex.createIndex({"family" : 1}, { "name" : "ix_Patient_family", partialFilterExpression : { "internal_resource" : "Patient" }, "background" : "true" }));

print("specific index for Encounter.serviceprovider");
printjson(db.searchindex.createIndex({"internal_resource" : 1, "serviceprovider" : 1}, { "name" : "ix_Encounter_serviceProvider", partialFilterExpression : { "internal_resource" : "Encounter" }, "background" : "true" }));
print("specific index for references to patient, from any resources that has a 'patient' search parameter");
printjson(db.searchindex.createIndex({"internal_resource" : 1, "patient" : 1}, { "name" : "ix_patient_reference", "background" : "true" }));
print("specific index for Observation.code");  
printjson(db.searchindex.createIndex({"code.code" : 1, "code.system" : 1}, { "name" : "ix_Observation_code", partialFilterExpression : { "internal_resource" : "Observation" }, "background" : "true" }));
