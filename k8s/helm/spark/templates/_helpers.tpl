{{/*
Expand the name of the chart.
*/}}
{{- define "spark.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "spark.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "spark.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "spark.labels" -}}
helm.sh/chart: {{ include "spark.chart" . }}
{{ include "spark.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "spark.selectorLabels" -}}
app.kubernetes.io/name: {{ include "spark.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "spark.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "spark.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
MongoDB full name
*/}}
{{- define "spark.mongodb.fullname" -}}
{{- printf "%s-mongodb" (include "spark.fullname" .) | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
MongoDB labels
*/}}
{{- define "spark.mongodb.labels" -}}
helm.sh/chart: {{ include "spark.chart" . }}
{{ include "spark.mongodb.selectorLabels" . }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
MongoDB selector labels
*/}}
{{- define "spark.mongodb.selectorLabels" -}}
app.kubernetes.io/name: {{ include "spark.mongodb.fullname" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: database
{{- end }}

{{/*
MongoDB secret name (uses existingSecret if set, otherwise generated name)
*/}}
{{- define "spark.mongodb.secretName" -}}
{{- if .Values.mongodb.auth.existingSecret }}
{{- .Values.mongodb.auth.existingSecret }}
{{- else }}
{{- printf "%s-credentials" (include "spark.mongodb.fullname" .) }}
{{- end }}
{{- end }}

{{/*
MongoDB connection string
*/}}
{{- define "spark.mongodb.connectionString" -}}
{{- if .Values.mongodb.enabled }}
{{- printf "mongodb://%s:%s@%s.%s.svc.cluster.local:%d/%s?authSource=admin" (.Values.mongodb.auth.username | urlquery) (.Values.mongodb.auth.password | urlquery) (include "spark.mongodb.fullname" .) .Release.Namespace (int .Values.mongodb.port) .Values.mongodb.auth.database }}
{{- else }}
{{- if not .Values.externalMongodb.connectionString }}
{{- fail "externalMongodb.connectionString is required when mongodb.enabled=false" }}
{{- end }}
{{- .Values.externalMongodb.connectionString }}
{{- end }}
{{- end }}
