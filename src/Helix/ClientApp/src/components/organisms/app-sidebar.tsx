import { Button } from "../ui/button";
import { Sidebar, SidebarContent, SidebarGroup, SidebarGroupContent, SidebarGroupLabel, SidebarHeader } from "../ui/sidebar";

export default function AppSidebar() {
    return (
        <Sidebar>
            <SidebarHeader>
                <div className="px-4 py-2 flex flex-row items-center gap-2">
                    <div className="w-8 h-8 bg-black foreground-white text-white rounded p-1">
                        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" strokeLinecap="round" strokeLinejoin="round" className="lucide lucide-dna-icon lucide-dna"><path d="m10 16 1.5 1.5" /><path d="m14 8-1.5-1.5" /><path d="M15 2c-1.798 1.998-2.518 3.995-2.807 5.993" /><path d="m16.5 10.5 1 1" /><path d="m17 6-2.891-2.891" /><path d="M2 15c6.667-6 13.333 0 20-6" /><path d="m20 9 .891.891" /><path d="M3.109 14.109 4 15" /><path d="m6.5 12.5 1 1" /><path d="m7 18 2.891 2.891" /><path d="M9 22c1.798-1.998 2.518-3.995 2.807-5.993" /></svg>
                    </div>
                    <h1 className="font-bold text-xl">Helix</h1>
                </div>
            </SidebarHeader>
            <SidebarContent>
                <div className="px-4">
                    <Button variant="outline" className="w-full">New Task</Button>
                </div>
                <SidebarGroup>
                    <SidebarGroupLabel>Tasks</SidebarGroupLabel>
                    <SidebarGroupContent>

                    </SidebarGroupContent>
                </SidebarGroup>
            </SidebarContent>
        </Sidebar>
    )
}