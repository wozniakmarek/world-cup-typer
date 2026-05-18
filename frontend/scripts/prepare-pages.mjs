import { copyFile, mkdir } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const currentDirectory = path.dirname(fileURLToPath(import.meta.url))
const frontendRoot = path.resolve(currentDirectory, '..')
const distDirectory = path.join(frontendRoot, 'dist')
const indexHtmlPath = path.join(distDirectory, 'index.html')
const notFoundHtmlPath = path.join(distDirectory, '404.html')

await mkdir(distDirectory, { recursive: true })
await copyFile(indexHtmlPath, notFoundHtmlPath)

console.log('Prepared GitHub Pages SPA fallback: dist/404.html')
