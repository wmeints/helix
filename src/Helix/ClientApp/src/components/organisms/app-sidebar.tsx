import { Dna } from "lucide-react"
import { Button } from "../ui/button"
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
} from "../ui/sidebar"

export default function AppSidebar() {
  return (
    <Sidebar>
      <SidebarHeader>
        <div className="px-3 py-2 flex flex-row items-center gap-2">
          <div className="w-8 h-8 bg-black foreground-white text-white rounded p-1">
            <Dna />
          </div>
          <h1 className="font-bold text-xl">Helix</h1>
        </div>
      </SidebarHeader>
      <SidebarContent>
        <div className="px-4">
          <Button variant="outline" className="w-full">
            New Task
          </Button>
        </div>
        <SidebarGroup>
          <SidebarGroupLabel>Tasks</SidebarGroupLabel>
          <SidebarGroupContent></SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  )
}
