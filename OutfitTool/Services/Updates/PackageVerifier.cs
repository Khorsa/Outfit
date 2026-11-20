/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Security.Cryptography;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.Services.Updates;

/// <summary>
/// Проверка целостности и подписи пакета обновления.
/// </summary>
public static class PackageVerifier
{
    public static void Verify(RepositoryItem item, byte[] bytes, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(logger);

        if (!string.IsNullOrWhiteSpace(item.Signature))
        {
            if (string.IsNullOrWhiteSpace(item.PublicKey))
            {
                throw new InvalidOperationException(
                    $"Для пакета {item.AssemblyName} указана подпись, но не задан публичный ключ.");
            }

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(item.PublicKey);
                var signature = Convert.FromBase64String(item.Signature.Trim());

                if (!rsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                {
                    throw new InvalidOperationException(
                        $"Подпись пакета {item.AssemblyName} недействительна.");
                }

                logger.Info($"Подпись пакета {item.AssemblyName} проверена.");
                return;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Не удалось проверить подпись пакета {item.AssemblyName}: {ex.Message}", ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(item.PublicKey))
        {
            throw new InvalidOperationException(
                $"Для пакета {item.AssemblyName} отсутствует подпись, хотя задан публичный ключ.");
        }

        if (string.IsNullOrWhiteSpace(item.Sha256))
        {
            logger.Warning($"Для пакета {item.AssemblyName} не указан sha256; проверка целостности пропущена.");
            return;
        }

        var actual = Convert.ToHexString(SHA256.HashData(bytes));
        if (!string.Equals(actual, item.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Контрольная сумма пакета {item.AssemblyName} не совпадает.");
        }
    }
}
