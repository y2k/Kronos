FROM microsoft/dotnet:latest

COPY . /app
WORKDIR /app
RUN dotnet restore

CMD ["/bin/bash", "-c", "dotnet run $KR_TLG_TOKEN $KR_TLG_CHAT $KR_GF_TOKEN $KR_GF_OWNER $KR_GF_REPO"]