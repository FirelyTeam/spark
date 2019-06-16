print("Removing duplicate Ids");
printjson(db.resources.update({}, {$unset: {Id:1}}, {multi: true}));
